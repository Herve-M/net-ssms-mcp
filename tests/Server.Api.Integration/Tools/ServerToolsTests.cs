using System.Text.Json;
using ModelContextProtocol.Protocol;
using ssms.Server.Api.Integration.Tests.Fixtures;

namespace Server.Api.Integration.Tests;

public class ServerToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{

    private readonly AspireContext _aspireContext = aspireContext;

    [Fact]
    public async Task ListToolsAsync_ShouldSucceed()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        // Act
        var toolList = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(toolList);
    }

    [Fact]
    public async Task GetServersList_ShouldSucceed()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        // Act
        CallToolResult serverListResult = await mcpClient.CallToolAsync("get_servers_list", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(serverListResult);
        Assert.Null(serverListResult.IsError);
        Assert.NotEmpty(serverListResult.Content);
        Assert.IsAssignableFrom<TextContentBlock>(serverListResult.Content.First());
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerVersion_PerServer_ShouldSucceed(string serverName)
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_version",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        // Act
        CallToolResult serverVersionResult = await mcpClient
            .CallToolAsync(
                requestParams,
                cancellationToken: TestContext.Current.CancellationToken
            );

        // Assert
        Assert.NotNull(serverVersionResult);
        Assert.Null(serverVersionResult.IsError);
        Assert.NotEmpty(serverVersionResult.Content);
        Assert.IsAssignableFrom<TextContentBlock>(serverVersionResult.Content.First());
    }
}
