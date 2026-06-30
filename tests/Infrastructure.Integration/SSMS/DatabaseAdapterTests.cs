using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.Integration.Fixtures;
using ssmsmcp.Infrastructure.SSMS;
using DatabaseAdapter = ssmsmcp.Infrastructure.SSMS.DatabaseAdapter;
using SqlServerVersion = ssmsmcp.Infrastructure.Integration.Fixtures.SqlServerVersion;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class DatabaseAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private DatabaseAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        return new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()));
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetDatabaseObjects_ForTables_ReturnsOnlyTableRows(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        DatabaseAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<DatabaseObjectInfo> objects = await adapter.GetDatabaseObjects(
            DataSourceName, spec.DatabaseName, DatabaseObjectTypes.Table, forceRefresh: false, TestContext.Current.CancellationToken);

        // Assert
        objects.Should().NotBeEmpty();
        objects.Should().OnlyContain(o => o.Type == "Table");
        objects.Should().OnlyContain(o => !string.IsNullOrEmpty(o.Schema));
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetDatabaseObjects_ForMissingDatabase_ReturnsEmpty(SqlServerVersion version)
    {
        // Arrange
        DatabaseAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<DatabaseObjectInfo> objects = await adapter.GetDatabaseObjects(
            DataSourceName, "DoesNotExist_DB", DatabaseObjectTypes.Table, forceRefresh: false, TestContext.Current.CancellationToken);

        // Assert
        objects.Should().BeEmpty();
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetDatabaseObjects_WhenCalledAgain_ServesCachedResultUntilForceRefresh(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        DatabaseAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<DatabaseObjectInfo> first = await adapter.GetDatabaseObjects(
            DataSourceName, spec.DatabaseName, DatabaseObjectTypes.Table, forceRefresh: false, TestContext.Current.CancellationToken);
        IReadOnlyCollection<DatabaseObjectInfo> cached = await adapter.GetDatabaseObjects(
            DataSourceName, spec.DatabaseName, DatabaseObjectTypes.Table, forceRefresh: false, TestContext.Current.CancellationToken);
        IReadOnlyCollection<DatabaseObjectInfo> refreshed = await adapter.GetDatabaseObjects(
            DataSourceName, spec.DatabaseName, DatabaseObjectTypes.Table, forceRefresh: true, TestContext.Current.CancellationToken);

        // Assert
        cached.Should().BeSameAs(first);
        refreshed.Should().NotBeSameAs(first);
        refreshed.Should().BeEquivalentTo(first);
        factory.Dispose();
    }
}
