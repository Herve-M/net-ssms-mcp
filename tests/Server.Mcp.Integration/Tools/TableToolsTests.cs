using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class TableToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    private static string DatabaseFor(string sqlResource) => sqlResource switch
    {
        AspireContext.Sql2022Resource => "AdventureWorksLT2022",
        AspireContext.Sql2025Resource => "AdventureWorksLT2025",
        _ => throw new ArgumentOutOfRangeException(nameof(sqlResource), sqlResource, "Unknown SQL resource."),
    };

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeTable_ForTableWithOutboundForeignKeysAndChecks_ReturnsFullShape(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_table",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
                ["name"] = JsonSerializer.SerializeToElement("SalesOrderDetail"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        content.GetProperty("columns").GetArrayLength().Should().BeGreaterThan(0);
        content.GetProperty("primaryKey").ValueKind.Should().NotBe(JsonValueKind.Null);
        content.GetProperty("foreignKeysOutbound").GetArrayLength().Should().Be(2);
        content.GetProperty("checkConstraints").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeTable_ForTableWithInboundForeignKeys_ReturnsInboundArray(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_table",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
                ["name"] = JsonSerializer.SerializeToElement("Product"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        content.GetProperty("foreignKeysInbound").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeTable_ForTableWithoutForeignKeys_ReturnsEmptyForeignKeyArrays(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_table",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement("BuildVersion"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        content.GetProperty("foreignKeysOutbound").GetArrayLength().Should().Be(0);
        content.GetProperty("foreignKeysInbound").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeTable_WithIncludeStatistics_ReturnsStatisticsWithNullFreshnessFields(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_table",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
                ["name"] = JsonSerializer.SerializeToElement("Product"),
                ["include_statistics"] = JsonSerializer.SerializeToElement(true),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        JsonElement statistics = content.GetProperty("statistics");
        statistics.GetArrayLength().Should().BeGreaterThan(0);
        statistics[0].GetProperty("lastUpdated").ValueKind.Should().Be(JsonValueKind.Null);
        statistics[0].GetProperty("rowsSampled").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeTable_ForMissingTable_ReturnsError(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_table",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement("DoesNotExist_Table"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
    }
}
