using AwesomeAssertions;
using ModelContextProtocol.Protocol;
using ssms.Server.Api.Integration.Tests.Fixtures;

namespace Server.Api.Integration.Tests;

public class HostTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    // [Fact]
    // public async Task Integration_Asprire_Ok()
    // {
    //     // Arrange && Act
    //     await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);

    //     // Assert
    //     ResourceEvent ressourceEvent = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
    //     await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(ressourceEvent.Resource, TestContext.Current.CancellationToken);
    // }

    [Fact]
    public async Task ApiHost_Healthcheck_ShouldSucceed()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        using var httpClient = _aspireContext.Context.CreateHttpClient("http-api");


        // Act
        HttpResponseMessage response = await httpClient.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Mcp_Ping_ShouldSucceed()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        // Act
        PingResult pong = await mcpClient.PingAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        pong.Should().NotBeNull();
    }
}
