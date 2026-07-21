using AwesomeAssertions;
using Microsoft.Data.SqlClient;
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

public sealed class StoredProcedureAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private StoredProcedureAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()));
        return new StoredProcedureAdapter(databasePort);
    }

    private async Task CreateTestProcedureAsync(SqlServerVersion version, string procedureName, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(fixture.GetConnectionString(version));
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(
            $"IF OBJECT_ID('dbo.{procedureName}') IS NOT NULL DROP PROCEDURE dbo.{procedureName}; " +
            $"EXEC('CREATE PROCEDURE dbo.{procedureName} @Id INT AS SELECT @Id AS Id')",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetStoredProcedure_ForExistingProcedure_ReturnsProcedureWithParameters(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        const string procedureName = "DescribeProcTest_Sp";
        await CreateTestProcedureAsync(version, procedureName, TestContext.Current.CancellationToken);
        StoredProcedureAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        StoredProcedure? procedure = await adapter.GetStoredProcedure(
            DataSourceName, spec.DatabaseName, "dbo", procedureName, TestContext.Current.CancellationToken);

        // Assert
        procedure.Should().NotBeNull();
        procedure!.Name.Should().Be(procedureName);
        procedure.Parameters.Count.Should().Be(1);
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetStoredProcedure_ForMissingProcedure_ReturnsNull(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        StoredProcedureAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        StoredProcedure? procedure = await adapter.GetStoredProcedure(
            DataSourceName, spec.DatabaseName, "dbo", "DoesNotExist_Procedure", TestContext.Current.CancellationToken);

        // Assert
        procedure.Should().BeNull();
        factory.Dispose();
    }
}
