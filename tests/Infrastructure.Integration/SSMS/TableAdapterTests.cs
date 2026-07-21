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

public sealed class TableAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private TableAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()));
        return new TableAdapter(databasePort);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetTable_ForExistingTable_ReturnsTableWithColumns(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        TableAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        Table? table = await adapter.GetTable(
            DataSourceName, spec.DatabaseName, "SalesLT", "Product", TestContext.Current.CancellationToken);

        // Assert
        table.Should().NotBeNull();
        table!.Name.Should().Be("Product");
        table.Schema.Should().Be("SalesLT");
        table.Columns.Count.Should().BeGreaterThan(0);
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetTable_ForMissingTable_ReturnsNull(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        TableAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        Table? table = await adapter.GetTable(
            DataSourceName, spec.DatabaseName, "dbo", "DoesNotExist_Table", TestContext.Current.CancellationToken);

        // Assert
        table.Should().BeNull();
        factory.Dispose();
    }
}
