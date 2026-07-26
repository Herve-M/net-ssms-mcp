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

public sealed class UserDefinedAggregateAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private UserDefinedAggregateAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()), NullLogger<DatabaseAdapter>.Instance);
        return new UserDefinedAggregateAdapter(databasePort);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetUserDefinedAggregate_ForMissingAggregate_ReturnsNull(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        UserDefinedAggregateAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        UserDefinedAggregate? aggregate = await adapter.GetUserDefinedAggregate(
            DataSourceName, spec.DatabaseName, "dbo", "DoesNotExist_Aggregate", TestContext.Current.CancellationToken);

        // Assert
        aggregate.Should().BeNull();
        factory.Dispose();
    }
}
