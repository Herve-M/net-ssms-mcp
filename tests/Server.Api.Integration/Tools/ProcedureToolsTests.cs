using System.Text.Json;
using Aspire.Hosting;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssms.Server.Api.Integration.Tests.Fixtures;

namespace Server.Api.Integration.Tests;

public class ProcedureToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    private static string DatabaseFor(string serverName) => serverName switch
    {
        AspireContext.Sql2022ServerName => "AdventureWorksLT2022",
        AspireContext.Sql2025ServerName => "AdventureWorksLT2025",
        _ => throw new ArgumentOutOfRangeException(nameof(serverName), serverName, "Unknown server name."),
    };

    // See AspireContext.Sql2022ResourceId/Sql2025ResourceId for the distinction between Aspire
    // resource ids and the "2022"/"2025" MCP server_name values.
    private static string AspireResourceIdFor(string serverName) => serverName switch
    {
        AspireContext.Sql2022ServerName => AspireContext.Sql2022ResourceId,
        AspireContext.Sql2025ServerName => AspireContext.Sql2025ResourceId,
        _ => throw new ArgumentOutOfRangeException(nameof(serverName), serverName, "Unknown server name."),
    };

    private async Task<string> GetRawConnectionStringAsync(string serverName, CancellationToken cancellationToken)
    {
        await _aspireContext.EnsureStartedAsync(cancellationToken);
        string resourceId = AspireResourceIdFor(serverName);
        return await _aspireContext.Context.GetConnectionStringAsync(resourceId, cancellationToken)
            ?? throw new InvalidOperationException($"No connection string available for server '{serverName}'.");
    }

    private async Task CreateTestProcedureAsync(string serverName, string procedureName, CancellationToken cancellationToken)
    {
        string connectionString = await GetRawConnectionStringAsync(serverName, cancellationToken);
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(
            $"IF OBJECT_ID('dbo.{procedureName}') IS NOT NULL DROP PROCEDURE dbo.{procedureName}; " +
            $"EXEC('CREATE PROCEDURE dbo.{procedureName} AS SELECT 1 AS Id')",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022ServerName)]
    [InlineData(AspireContext.Sql2025ServerName)]
    public async Task DescribeProcedure_WithServerNameAndDatabase_ShouldSucceed(string serverName)
    {
        const string procedureName = "DescribeProcApiTest_Sp";
        await CreateTestProcedureAsync(serverName, procedureName, TestContext.Current.CancellationToken);

        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_procedure",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["server_name"] = JsonSerializer.SerializeToElement(serverName),
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(serverName)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement(procedureName),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DescribeProcedure_WithoutServerName_ShouldError()
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_procedure",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement("AdventureWorksLT2022"),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement("SomeProcedure"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(AspireContext.Sql2022ServerName)]
    [InlineData(AspireContext.Sql2025ServerName)]
    public async Task DescribeProcedure_ForMissingObject_ShouldError(string serverName)
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_procedure",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["server_name"] = JsonSerializer.SerializeToElement(serverName),
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(serverName)),
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
