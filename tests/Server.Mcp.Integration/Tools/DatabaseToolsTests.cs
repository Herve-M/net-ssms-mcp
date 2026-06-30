using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class DatabaseToolsTests(AspireContext aspireContext)
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
    public async Task ListObjects_WithoutServerName_ShouldDefaultToMain(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_objects",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task ListObjects_FilteredByViewAndSchema_ShouldSucceed(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_objects",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["object_type"] = JsonSerializer.SerializeToElement("VIEW"),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }
}
