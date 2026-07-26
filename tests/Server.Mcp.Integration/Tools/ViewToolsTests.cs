using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class ViewToolsTests(AspireContext aspireContext)
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
    public async Task DescribeView_ForExistingView_ReturnsColumnsAndDefinition(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_view",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
                ["name"] = JsonSerializer.SerializeToElement("vGetAllCategories"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.StructuredContent.Should().NotBeNull();

        JsonElement content = result.StructuredContent!.Value;
        content.GetProperty("columns").GetArrayLength().Should().BeGreaterThan(0);
        content.GetProperty("hasIndex").GetBoolean().Should().BeFalse();
        content.GetProperty("isEncrypted").GetBoolean().Should().BeFalse();
        content.GetProperty("definition").GetString().Should().Contain("SELECT");
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task DescribeView_ForMissingView_ReturnsError(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_view",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["schema"] = JsonSerializer.SerializeToElement("dbo"),
                ["name"] = JsonSerializer.SerializeToElement("DoesNotExist_View"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
    }
}
