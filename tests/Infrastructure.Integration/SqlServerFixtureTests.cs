using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using ssmsmcp.Infrastructure.Integration.Fixtures;

namespace ssmsmcp.Infrastructure.Integration;

public sealed class SqlServerFixtureTests(SqlServerFixture fixture)
{
    [Theory]
    [InlineData(SqlServerVersion.Sql2022, 2022)]
    [InlineData(SqlServerVersion.Sql2025, 2025)]
    public async Task Container_is_reachable_and_reports_expected_version(
        SqlServerVersion version, int expectedYear)
    {
        await using SqlConnection connection = new(fixture.GetConnectionString(version));
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using SqlCommand command = new("SELECT @@VERSION", connection);
        object? result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        result.Should().BeOfType<string>()
            .Which.Should().Contain(expectedYear.ToString());
    }
}
