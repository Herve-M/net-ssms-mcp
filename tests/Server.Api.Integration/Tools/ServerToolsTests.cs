using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssms.Server.Api.Integration.Tests.Fixtures;

namespace Server.Api.Integration.Tests;

public class ServerToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    [Fact]
    public async Task ListToolsAsync_ShouldExposeExactlyTheSpecTools()
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        IList<McpClientTool> toolList = await mcpClient
            .ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        toolList.Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "get_server_info", "list_databases", "list_objects" });
    }

    [Theory]
    [InlineData(AspireContext.Sql2022ServerName)]
    [InlineData(AspireContext.Sql2025ServerName)]
    public async Task GetServerInfo_WithServerName_ShouldSucceed(string serverName)
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "get_server_info",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["server_name"] = JsonSerializer.SerializeToElement(serverName),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(AspireContext.Sql2022ServerName)]
    [InlineData(AspireContext.Sql2025ServerName)]
    public async Task ListDatabases_WithServerName_ShouldSucceed(string serverName)
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_databases",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["server_name"] = JsonSerializer.SerializeToElement(serverName),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetServerInfo_WithoutServerName_ShouldError()
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolResult result = await mcpClient
            .CallToolAsync("get_server_info", cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
    }
}
