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

public sealed class RoleMembershipAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private RoleMembershipAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });

        IServerPort serverPort = new ServerAdapter(factory);
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()), NullLogger<DatabaseAdapter>.Instance);

        return new RoleMembershipAdapter(serverPort, databasePort);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetServerRoleMemberships_ForRunningInstance_ReturnsCollection(SqlServerVersion version)
    {
        // Arrange
        RoleMembershipAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<RoleMembershipEdge> edges = await adapter.GetServerRoleMemberships(
            DataSourceName, TestContext.Current.CancellationToken);

        // Assert
        edges.Should().NotBeNull();
        edges.Should().OnlyContain(e => e.Scope == "SERVER");
        edges.Should().OnlyContain(e => e.RoleTypeDesc == "SERVER_ROLE");
        edges.Should().OnlyContain(e => e.Database == null);
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetDatabaseRoleMemberships_ForAdventureWorksLT_ReturnsCollection(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        RoleMembershipAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<RoleMembershipEdge> edges = await adapter.GetDatabaseRoleMemberships(
            DataSourceName, spec.DatabaseName, TestContext.Current.CancellationToken);

        // Assert
        edges.Should().NotBeNull();
        edges.Should().OnlyContain(e => e.Scope == "DATABASE");
        edges.Should().OnlyContain(e => e.Database == spec.DatabaseName);
        edges.Should().OnlyContain(e => e.RoleTypeDesc == "DATABASE_ROLE");

        if (edges.Any(e => e.Role == "db_owner"))
        {
            edges.Should().Contain(e => e.Role == "db_owner" && e.MemberTypeDesc != "UNKNOWN");
        }

        factory.Dispose();
    }
}
