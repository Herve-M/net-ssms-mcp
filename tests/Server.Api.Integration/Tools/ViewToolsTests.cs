using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssms.Server.Api.Integration.Tests.Fixtures;

namespace Server.Api.Integration.Tests;

public class ViewToolsTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    private static string DatabaseFor(string serverName) => serverName switch
    {
        AspireContext.Sql2022ServerName => "AdventureWorksLT2022",
        AspireContext.Sql2025ServerName => "AdventureWorksLT2025",
        _ => throw new ArgumentOutOfRangeException(nameof(serverName), serverName, "Unknown server name."),
    };

    [Theory]
    [InlineData(AspireContext.Sql2022ServerName)]
    [InlineData(AspireContext.Sql2025ServerName)]
    public async Task DescribeView_WithServerNameAndDatabase_ShouldSucceed(string serverName)
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_view",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["server_name"] = JsonSerializer.SerializeToElement(serverName),
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(serverName)),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
                ["name"] = JsonSerializer.SerializeToElement("vGetAllCategories"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DescribeView_WithoutServerName_ShouldError()
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_view",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement("AdventureWorksLT2022"),
                ["schema"] = JsonSerializer.SerializeToElement("SalesLT"),
                ["name"] = JsonSerializer.SerializeToElement("vGetAllCategories"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(AspireContext.Sql2022ServerName)]
    [InlineData(AspireContext.Sql2025ServerName)]
    public async Task DescribeView_ForMissingView_ShouldError(string serverName)
    {
        await using var mcpClient = await _aspireContext
            .GetMcpClientWhenReadyAsync(TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "describe_view",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["server_name"] = JsonSerializer.SerializeToElement(serverName),
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(serverName)),
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
