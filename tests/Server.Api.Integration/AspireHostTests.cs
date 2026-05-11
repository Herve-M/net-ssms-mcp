using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ssms.Server.Api.Integration.Tests.Fixtures;

namespace Server.Api.Integration.Tests;

public class IntegrationTest1(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{

    private readonly AspireContext _aspireContext = aspireContext;

    [Fact]
    public async Task Integration_Asprire_Ok()
    {
        // Arrange && Act
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        ResourceEvent ressourceEvent = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(ressourceEvent.Resource, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCodeWithRetry()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);

        // mcp http client
        var mcpHttpClient = _aspireContext.Context.CreateHttpClient("http-api");
        var httpClientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{mcpHttpClient.BaseAddress}mcp"),
            TransportMode = HttpTransportMode.AutoDetect,
        }, mcpHttpClient);
        await using var mcpClient = await McpClient.CreateAsync(httpClientTransport, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var toolList = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(toolList);
    }
}
