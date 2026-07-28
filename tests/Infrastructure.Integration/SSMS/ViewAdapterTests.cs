using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.Integration.Fixtures;
using ssmsmcp.Infrastructure.SSMS;
using DatabaseAdapter = ssmsmcp.Infrastructure.SSMS.DatabaseAdapter;
using SqlServerVersion = ssmsmcp.Infrastructure.Integration.Fixtures.SqlServerVersion;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class ViewAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private ViewAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()), NullLogger<DatabaseAdapter>.Instance);
        return new ViewAdapter(databasePort);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetView_ForExistingView_ReturnsViewWithColumns(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        ViewAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        View? view = await adapter.GetView(
            DataSourceName, spec.DatabaseName, "SalesLT", "vGetAllCategories", TestContext.Current.CancellationToken);

        // Assert
        view.Should().NotBeNull();
        view!.Name.Should().Be("vGetAllCategories");
        view.Schema.Should().Be("SalesLT");
        view.Columns.Count.Should().BeGreaterThan(0);
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetView_ForMissingView_ReturnsNull(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        ViewAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        View? view = await adapter.GetView(
            DataSourceName, spec.DatabaseName, "dbo", "DoesNotExist_View", TestContext.Current.CancellationToken);

        // Assert
        view.Should().BeNull();
        factory.Dispose();
    }
}
