using AwesomeAssertions;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.Integration.Fixtures;
using SqlServerVersion = ssmsmcp.Infrastructure.Integration.Fixtures.SqlServerVersion;

namespace ssmsmcp.Infrastructure.Integration.SSMS;

public sealed class ServerConnectionFactoryTests(SqlServerFixture fixture)
{
    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task ConnectTo_WithLiveServer_ReturnsServerWithExpectedMajorVersion(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        using IServerConnectionFactory factory = fixture.CreateFactory();

        // Act
        Server server = await factory.ConnectTo(fixture.GetConnectionString(version));

        // Assert
        server.Version.Major.Should().Be(spec.ExpectedMajorVersion);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task ConnectTo_CalledTwiceForSameDataSource_ReturnsCachedConnection(SqlServerVersion version)
    {
        // Arrange
        using IServerConnectionFactory factory = fixture.CreateFactory();
        string connectionString = fixture.GetConnectionString(version);

        // Act
        Server first = await factory.ConnectTo(connectionString);
        Server second = await factory.ConnectTo(connectionString);

        // Assert
        // The factory caches the underlying ServerConnection per data source and wraps it
        // in a fresh Server on cache hits, so the connection context must be the same instance.
        second.ConnectionContext.Should().BeSameAs(first.ConnectionContext);
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022)]
    [InlineData(SqlServerVersion.Sql2025)]
    public async Task ConnectTo_WithRestoredBackup_ExposesAdventureWorksDatabase(SqlServerVersion version)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        using IServerConnectionFactory factory = fixture.CreateFactory();

        // Act
        Server server = await factory.ConnectTo(fixture.GetConnectionString(version));

        // Assert
        server.Databases.Contains(spec.DatabaseName).Should().BeTrue();
    }

    [Theory]
    [InlineData(SqlServerVersion.Sql2022, "sql-2022")]
    [InlineData(SqlServerVersion.Sql2025, "sql-2025")]
    public async Task GetServer_WithConfiguredDataSource_ResolvesServerWithExpectedMajorVersion(
        SqlServerVersion version, string name)
    {
        // Arrange
        SqlServerImageSpec spec = SqlServerImageSpec.For(version);
        using IServerConnectionFactory factory = fixture.CreateFactory(
            new DataSource { Name = name, ConnectionString = fixture.GetConnectionString(version) });

        // Act
        Server server = await factory.GetServer(name);

        // Assert
        server.Version.Major.Should().Be(spec.ExpectedMajorVersion);
    }

    [Fact]
    public async Task GetServer_WithUnknownServerName_ThrowsInvalidOperationException()
    {
        // Arrange
        using IServerConnectionFactory factory = fixture.CreateFactory(
            new DataSource { Name = "sql-2022", ConnectionString = fixture.GetConnectionString(SqlServerVersion.Sql2022) });

        // Act
        Func<Task> act = async () => await factory.GetServer("does-not-exist");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllServers_AfterConnectingConfiguredDataSources_ReturnsAllServers()
    {
        // Arrange
        DataSource ds2022 = new()
        {
            Name = "sql-2022",
            ConnectionString = fixture.GetConnectionString(SqlServerVersion.Sql2022),
        };
        DataSource ds2025 = new()
        {
            Name = "sql-2025",
            ConnectionString = fixture.GetConnectionString(SqlServerVersion.Sql2025),
        };
        using IServerConnectionFactory factory = fixture.CreateFactory(ds2022, ds2025);

        // Act
        // GetAllServers only returns servers populated on the ConnectTo create-path, so connect first.
        _ = await factory.GetServer("sql-2022");
        _ = await factory.GetServer("sql-2025");
        IReadOnlyCollection<Server> all = await factory.GetAllServers();

        // Assert
        all.Should().HaveCount(2);
    }
}
