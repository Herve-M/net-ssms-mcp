using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration.Tools;

public class SecurityToolsTests(AspireContext aspireContext)
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
    public async Task ListPrincipals_WithDatabaseScope_ShouldSucceed(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_principals",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["scope"] = JsonSerializer.SerializeToElement("BOTH"),
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
    public async Task ListPrincipals_WithoutServerName_ShouldDefaultToMain(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_principals",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["scope"] = JsonSerializer.SerializeToElement("SERVER"),
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
    public async Task ListRoleMemberships_DefaultScope_ShouldSucceed(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_role_memberships",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["include_system"] = JsonSerializer.SerializeToElement(true),
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
    public async Task ListPermissions_DatabaseSecurable_ShouldSucceed(string sqlResource)
    {
        await using McpClient mcpClient = await _aspireContext
            .GetStdioMcpClientAsync(sqlResource, TestContext.Current.CancellationToken);

        CallToolRequestParams requestParams = new()
        {
            Name = "list_permissions",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["database"] = JsonSerializer.SerializeToElement(DatabaseFor(sqlResource)),
                ["securable_type"] = JsonSerializer.SerializeToElement("DATABASE"),
            },
        };

        CallToolResult result = await mcpClient
            .CallToolAsync(requestParams, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsError.Should().BeNull();
        result.Content.Should().NotBeEmpty();
    }
}
