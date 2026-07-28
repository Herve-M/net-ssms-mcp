using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Abstractions.Servers;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.Integration.Fixtures;
using ssmsmcp.Infrastructure.SSMS;
using DatabaseAdapter = ssmsmcp.Infrastructure.SSMS.DatabaseAdapter;
using ServerAdapter = ssmsmcp.Infrastructure.SSMS.ServerAdapter;
using SqlServerVersion = ssmsmcp.Infrastructure.Integration.Fixtures.SqlServerVersion;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class PrincipalAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private PrincipalAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });

        IServerPort serverPort = new ServerAdapter(factory);
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()), NullLogger<DatabaseAdapter>.Instance);

        return new PrincipalAdapter(serverPort, databasePort);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetServerLogins_ForRunningInstance_ReturnsNonEmptyCollection(SqlServerVersion version)
    {
        // Arrange
        PrincipalAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<Login> logins = await adapter.GetServerLogins(DataSourceName, TestContext.Current.CancellationToken);

        // Assert
        logins.Should().NotBeEmpty();
        logins.Should().Contain(l => l.Name == "sa");
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetDatabaseUsers_ForAdventureWorksLT_ReturnsNonEmptyCollection(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        PrincipalAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<User> users = await adapter.GetDatabaseUsers(
            DataSourceName, spec.DatabaseName, TestContext.Current.CancellationToken);

        // Assert
        users.Should().NotBeEmpty();
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetDatabaseRoles_ForAdventureWorksLT_IncludesDbOwner(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        PrincipalAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<DatabaseRole> roles = await adapter.GetDatabaseRoles(
            DataSourceName, spec.DatabaseName, TestContext.Current.CancellationToken);

        // Assert
        roles.Should().NotBeEmpty();
        roles.Should().Contain(r => r.Name == "db_owner");
        factory.Dispose();
    }
}
