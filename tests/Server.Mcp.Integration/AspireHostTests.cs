using Aspire.Hosting.Testing;
using AwesomeAssertions;
using ssmsmcp.Server.Mcp.Integration.Fixtures;

namespace ssmsmcp.Server.Mcp.Integration;

public class AspireHostTests(AspireContext aspireContext)
    : IClassFixture<AspireContext>
{
    private readonly AspireContext _aspireContext = aspireContext;

    [Theory]
    [InlineData(AspireContext.Sql2022Resource)]
    [InlineData(AspireContext.Sql2025Resource)]
    public async Task SqlServer_PerInstance_ShouldBecomeHealthy(string sqlResource)
    {
        // Act
        await _aspireContext.WaitForSqlAsync(sqlResource, TestContext.Current.CancellationToken);

        // Assert
        string? connectionString = await _aspireContext.Context
            .GetConnectionStringAsync(sqlResource, TestContext.Current.CancellationToken);
        connectionString.Should().NotBeNullOrWhiteSpace();
    }
}
