using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Abstractions.Security;
using ssmsmcp.Domain.Abstractions.Servers;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.Integration.Fixtures;
using ssmsmcp.Infrastructure.SSMS;
using DatabaseAdapter = ssmsmcp.Infrastructure.SSMS.DatabaseAdapter;
using ServerAdapter = ssmsmcp.Infrastructure.SSMS.ServerAdapter;
using SqlServerVersion = ssmsmcp.Infrastructure.Integration.Fixtures.SqlServerVersion;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class PermissionAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private PermissionAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });

        IServerPort serverPort = new ServerAdapter(factory);
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()), NullLogger<DatabaseAdapter>.Instance);

        return new PermissionAdapter(serverPort, databasePort);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetPermissions_DatabaseSecurable_ReturnsCollectionWithGrantRows(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        PermissionAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<PermissionRecord> records = await adapter.GetPermissions(
            DataSourceName, spec.DatabaseName, "DATABASE", TestContext.Current.CancellationToken);

        // Assert
        records.Should().NotBeNull();
        records.Should().OnlyContain(r => r.SecurableType == "DATABASE");
        records.Should().Contain(r => r.State == "GRANT");
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetPermissions_ObjectSecurable_RunsWithoutThrowing(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        PermissionAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<PermissionRecord> records = await adapter.GetPermissions(
            DataSourceName, spec.DatabaseName, "OBJECT", TestContext.Current.CancellationToken);

        // Assert
        records.Should().NotBeNull();
        records.Should().OnlyContain(r => r.SecurableType == "OBJECT");
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetPermissions_NullSecurableType_ReturnsUnionWithoutThrowing(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        PermissionAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<PermissionRecord> records = await adapter.GetPermissions(
            DataSourceName, spec.DatabaseName, null, TestContext.Current.CancellationToken);

        // Assert
        records.Should().NotBeNull();
        factory.Dispose();
    }
}
