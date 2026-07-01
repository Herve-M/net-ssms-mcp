using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class ServerToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task ListToolsAsync_ShouldExposeExactlyTheSpecTools(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        IList<McpClientTool> tools = await mcpClient
            .ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        tools.Select(t => t.Name)
            .Should().BeEquivalentTo(new[] { "get_server_info", "list_databases", "list_objects" });
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task GetServerInfo_WithoutServerName_ShouldDefaultToMain(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolResult result = await mcpClient
            .CallToolAsync("get_server_info", cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task ListDatabases_WithoutServerName_ShouldDefaultToMain(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolResult result = await mcpClient
            .CallToolAsync("list_databases", cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }
}
