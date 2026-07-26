using System.Text.Json;
using Aspire.Hosting.Testing;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class ProcedureToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    private static string DatabaseFor(string sqlResource) => sqlResource switch
    {
        AspireContext.Sql2022Resource => "AdventureWorksLT2022",
        AspireContext.Sql2025Resource => "AdventureWorksLT2025",
        _ => throw new ArgumentOutOfRangeException(nameof(sqlResource), sqlResource, "Unknown SQL resource."),
    };

    private async Task<string> GetRawConnectionStringAsync(string sqlResource, CancellationToken cancellationToken)
    {
        await _aspireContext.WaitForSqlAsync(sqlResource, cancellationToken);
        return await _aspireContext.Context.GetConnectionStringAsync(sqlResource, cancellationToken)
            ?? throw new InvalidOperationException($"No connection string available for resource '{sqlResource}'.");
    }

    private async Task CreateTestProcedureAsync(string sqlResource, string procedureName, CancellationToken cancellationToken)
    {
        string connectionString = await GetRawConnectionStringAsync(sqlResource, cancellationToken);
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(
            $"IF OBJECT_ID('dbo.{procedureName}') IS NOT NULL DROP PROCEDURE dbo.{procedureName}; " +
            $"EXEC('CREATE PROCEDURE dbo.{procedureName} @Id INT AS SELECT @Id AS Id, CAST(N''x'' AS NVARCHAR(50)) AS Name')",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateTestScalarFunctionAsync(string sqlResource, string functionName, CancellationToken cancellationToken)
    {
        string connectionString = await GetRawConnectionStringAsync(sqlResource, cancellationToken);
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(
            $"IF OBJECT_ID('dbo.{functionName}') IS NOT NULL DROP FUNCTION dbo.{functionName}; " +
            $"EXEC('CREATE FUNCTION dbo.{functionName} (@Id INT) RETURNS INT AS BEGIN RETURN @Id END')",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeProcedure_ForProcedureWithFirstResultSet_ReturnsFullShape(string sqlResource)
    {
        const string procedureName = "DescribeProcMcpTest_Sp";
        await CreateTestProcedureAsync(sqlResource, procedureName, TestContext.Current.CancellationToken);

        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_procedure",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement(procedureName),
                ["include_first_result_set"] = JsonSerializer.SerializeToElement(true),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        content.GetProperty("kind").GetString().Should().Be("PROCEDURE");
        content.GetProperty("parameters").GetArrayLength().Should().Be(1);
        content.GetProperty("firstResultSetColumns").GetArrayLength().Should().Be(2);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeProcedure_ForScalarFunction_ReturnsReturnType(string sqlResource)
    {
        const string functionName = "DescribeProcMcpTest_Fn";
        await CreateTestScalarFunctionAsync(sqlResource, functionName, TestContext.Current.CancellationToken);

        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_procedure",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement(functionName),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        content.GetProperty("kind").GetString().Should().Be("SCALAR_FUNCTION");
        content.GetProperty("returnType").GetString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeProcedure_ForMissingObject_ReturnsError(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_procedure",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement("DoesNotExist_Procedure"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
    }
}
