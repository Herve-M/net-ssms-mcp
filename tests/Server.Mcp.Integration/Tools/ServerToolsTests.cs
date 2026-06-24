using AwesomeAssertions;
using ModelContextProtocol.Client;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class ServerToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task ListToolsAsync_PerServer_ShouldExposeServerTools(string sqlResource)
    {
        // Arrange: launch a Server.Mcp stdio process targeting the given SQL Server instance.
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        // Act
        IList<McpClientTool> tools = await mcpClient
            .ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        tools.Should().NotBeNull();
        tools.Select(t => t.Name).Should().Contain("get_servers_list");
    }
}
