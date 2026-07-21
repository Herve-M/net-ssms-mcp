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

public sealed class UserDefinedFunctionAdapterTests(SqlServerFixture fixture)
{
    private const string DataSourceName = "primary";

    private UserDefinedFunctionAdapter CreateAdapter(SqlServerVersion version, out IServerConnectionFactory factory)
    {
        factory = fixture.CreateFactory(
            new DataSource { Name = DataSourceName, ConnectionString = fixture.GetConnectionString(version) });
        IDatabasePort databasePort = new DatabaseAdapter(factory, new MemoryCache(new MemoryCacheOptions()));
        return new UserDefinedFunctionAdapter(databasePort);
    }

    private async Task CreateTestFunctionAsync(SqlServerVersion version, string functionName, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = new(fixture.GetConnectionString(version));
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new(
            $"IF OBJECT_ID('dbo.{functionName}') IS NOT NULL DROP FUNCTION dbo.{functionName}; " +
            $"EXEC('CREATE FUNCTION dbo.{functionName} (@Id INT) RETURNS INT AS BEGIN RETURN @Id END')",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetUserDefinedFunction_ForExistingScalarFunction_ReturnsFunctionWithParameters(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        const string functionName = "DescribeProcTest_Fn";
        await CreateTestFunctionAsync(version, functionName, TestContext.Current.CancellationToken);
        UserDefinedFunctionAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        UserDefinedFunction? function = await adapter.GetUserDefinedFunction(
            DataSourceName, spec.DatabaseName, "dbo", functionName, TestContext.Current.CancellationToken);

        // Assert
        function.Should().NotBeNull();
        function!.Name.Should().Be(functionName);
        function.FunctionType.Should().Be(UserDefinedFunctionType.Scalar);
        function.Parameters.Count.Should().Be(1);
        factory.Dispose();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task GetUserDefinedFunction_ForMissingFunction_ReturnsNull(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        UserDefinedFunctionAdapter adapter = CreateAdapter(version, out IServerConnectionFactory factory);

        // Act
        UserDefinedFunction? function = await adapter.GetUserDefinedFunction(
            DataSourceName, spec.DatabaseName, "dbo", "DoesNotExist_Function", TestContext.Current.CancellationToken);

        // Assert
        function.Should().BeNull();
        factory.Dispose();
    }
}
