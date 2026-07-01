using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using ssmsmcp.Infrastructure.Integration.Fixtures;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class SqlServerFixtureTests(SqlServerFixture fixture)
{
    [Theory]
    [InlineData(SqlServerVersion.Sql2022, 2022)]
    [InlineData(SqlServerVersion.Sql2025, 2025)]
    public async Task GetConnectionString_ForRunningContainer_ConnectsAndReportsExpectedVersion(
        SqlServerVersion version, int expectedYear)
    {
        // Arrange
        await using SqlConnection connection = new(fixture.GetConnectionString(version));
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using SqlCommand command = new("SELECT @@VERSION", connection);

        // Act
        object? result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeOfType<string>()
            .Which.Should().Contain(expectedYear.ToString());
    }
}
