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
    public async Task ListToolsAsync_ShouldSucceed()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        // Act
        IList<McpClientTool> toolList = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        toolList.Should().NotBeNull();
        toolList.Select(x => x.Name).Should().NotContainInOrder(new[]
        {
            "get_servers_list",
            "get_server_version",
            "get_server_databases",
            "get_server_availability",
            "get_server_capacity",
            "get_server_connectivity",
            "get_server_overview",
            "get_server_engine",
            "get_server_features",
            "get_server_identity",
            "get_server_localization",
            "get_server_platform",
            "get_server_security",
            "get_server_storage"
        });
    }

    [Fact]
    public async Task GetServersList_ShouldSucceed()
    {
        // Arrange
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();


        // Act
        CallToolResult serverListResult = await mcpClient.CallToolAsync("get_servers_list", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        serverListResult.Should().NotBeNull();
        serverListResult.IsError.Should().BeNull();
        serverListResult.Content.Should().NotBeEmpty();
        serverListResult.Content.First().Should().BeAssignableTo<TextContentBlock>();
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
        serverVersionResult.Should().NotBeNull();
        serverVersionResult.IsError.Should().BeNull();
        serverVersionResult.Content.Should().NotBeEmpty();
        serverVersionResult.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerOverview_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_overview",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerAvailability_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_availability",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerCapacity_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_capacity",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerConnectivity_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_connectivity",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerDatabases_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_databases",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerEngine_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_engine",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerFeatures_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_features",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerIdentity_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_identity",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerLocalization_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_localization",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerPlatform_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_platform",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerSecurity_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_security",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("2025")]
    public async Task GetServerStorage_PerServer_ShouldSucceed(string serverName)
    {
        await _aspireContext.Context.StartAsync(TestContext.Current.CancellationToken);
        var apiR = await _aspireContext.Context.ResourceNotifications.WaitForResourceHealthyAsync("http-api", TestContext.Current.CancellationToken);
        await _aspireContext.Context.ResourceNotifications.WaitForDependenciesAsync(apiR.Resource, TestContext.Current.CancellationToken);
        await using var mcpClient = await _aspireContext.GetMcpClientAsync();

        CallToolRequestParams requestParams = new CallToolRequestParams
        {
            Name = "get_server_storage",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["serverName"] = JsonSerializer.SerializeToElement(serverName)
            }
        };

        CallToolResult result = await mcpClient.CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
        result.Content.First().Should().BeAssignableTo<TextContentBlock>();
    }
}
