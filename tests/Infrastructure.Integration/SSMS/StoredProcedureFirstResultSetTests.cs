using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.Integration.Fixtures;
using ssmsmcp.Infrastructure.SSMS;
using DatabaseAdapter = ssmsmcp.Infrastructure.SSMS.DatabaseAdapter;
using SqlServerVersion = ssmsmcp.Infrastructure.Integration.Fixtures.SqlServerVersion;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class StoredProcedureFirstResultSetTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private StoredProcedureAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()));
        return new StoredProcedureAdapter(databasePort);
    }

    private async Task<int> CreateTestProcedureWithResultSetAsync(SqlServerVersion version, string procedureName, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(fixture.GetConnectionString(version));
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand createCommand = new(
            $"IF OBJECT_ID('dbo.{procedureName}') IS NOT NULL DROP PROCEDURE dbo.{procedureName}; " +
            $"EXEC('CREATE PROCEDURE dbo.{procedureName} AS SELECT 1 AS Id, CAST(N''x'' AS NVARCHAR(50)) AS Name')",
            connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        await using SqlCommand idCommand = new($"SELECT OBJECT_ID('dbo.{procedureName}')", connection);
        object? result = await idCommand.ExecuteScalarAsync(cancellationToken);
        return (int)result!;
    }

    private async Task<int> CreateTestProcedureWithDynamicSqlAsync(SqlServerVersion version, string procedureName, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(fixture.GetConnectionString(version));
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand createCommand = new(
            $"IF OBJECT_ID('dbo.{procedureName}') IS NOT NULL DROP PROCEDURE dbo.{procedureName}; " +
            $"EXEC('CREATE PROCEDURE dbo.{procedureName} AS " +
            "BEGIN " +
            "DECLARE @sql NVARCHAR(MAX) = N''SELECT 1 AS Id''; " +
            "EXEC sp_executesql @sql; " +
            "END')",
            connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        await using SqlCommand idCommand = new($"SELECT OBJECT_ID('dbo.{procedureName}')", connection);
        object? result = await idCommand.ExecuteScalarAsync(cancellationToken);
        return (int)result!;
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task DescribeFirstResultSet_ForProcedureWithSelect_ReturnsColumns(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        const string procedureName = "DescribeProcTest_FirstResultSet";
        int objectId = await CreateTestProcedureWithResultSetAsync(version, procedureName, TestContext.Current.CancellationToken);
        StoredProcedureAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<FirstResultSetColumnInfo> columns = await adapter.DescribeFirstResultSet(
            DataSourceName, spec.DatabaseName, objectId, TestContext.Current.CancellationToken);

        // Assert
        columns.Should().HaveCount(2);
        columns.Should().Contain(c => c.Name == "Id");
        columns.Should().Contain(c => c.Name == "Name");
        columns.Should().OnlyContain(c => c.ErrorNumber == 0);
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task DescribeFirstResultSet_ForProcedureWithDynamicSql_ReturnsRowWithNonZeroErrorNumber(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        const string procedureName = "DescribeProcTest_FirstResultSet_DynamicSql";
        int objectId = await CreateTestProcedureWithDynamicSqlAsync(version, procedureName, TestContext.Current.CancellationToken);
        StoredProcedureAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        IReadOnlyCollection<FirstResultSetColumnInfo> columns = await adapter.DescribeFirstResultSet(
            DataSourceName, spec.DatabaseName, objectId, TestContext.Current.CancellationToken);

        // Assert
        columns.Should().Contain(c => c.ErrorNumber != 0);
        factory.Dispose();
    }
}
