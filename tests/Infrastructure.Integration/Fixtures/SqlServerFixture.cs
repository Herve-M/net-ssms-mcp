using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;
using ssmsmcp.Infrastructure.SSMS.Internals;

namespace ssmsmcp.Infrastructure.Integration.Fixtures;

/// <summary>
/// Builds and starts MS SQL Server 2022 and 2025 once per test assembly using the same
/// Dockerfiles the Aspire host uses (<c>src/Server.Aspire.Host/dockers</c>), restoring
/// the AdventureWorksLT backups from <c>tests/data</c>. Shared across every test class.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Meets SQL Server SA password complexity requirements.
    private const string SaPassword = "P@ssw0rd!Testcontainers";
    private const ushort MsSqlPort = 1433;

    private sealed record RunningInstance(IContainer Container, string ConnectionString);

    private readonly Dictionary<SqlServerVersion, RunningInstance> _instances = new();

    public async ValueTask InitializeAsync()
    {
        string gitRoot = CommonDirectoryPath.GetGitDirectory().DirectoryPath;
        string backupDir = Path.Combine(gitRoot, "tests", "data");

        // Build both images in parallel.
        IFutureDockerImage[] images = await Task.WhenAll(
            SqlServerImageSpec.All.Select(spec => BuildImageAsync(gitRoot, spec)));

        // Start both containers in parallel; order is preserved by Task.WhenAll.
        RunningInstance[] running = await Task.WhenAll(
            SqlServerImageSpec.All
                .Zip(images, (spec, image) => (spec, image))
                .Select(x => StartContainerAsync(x.spec, x.image, backupDir)));

        for (int i = 0; i < SqlServerImageSpec.All.Count; i++)
        {
            _instances[SqlServerImageSpec.All[i].Version] = running[i];
        }
    }

    private static async Task<IFutureDockerImage> BuildImageAsync(string gitRoot, SqlServerImageSpec spec)
    {
        string dockerfileDirectory = Path.Combine(gitRoot, "src", "Server.Aspire.Host", "dockers");

        IFutureDockerImage image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(dockerfileDirectory)
            .WithDockerfile(spec.Dockerfile)
            .WithName($"ssmsmcp-{spec.Version.ToString().ToLowerInvariant()}-test:latest")
            .WithDeleteIfExists(true)
            .WithCleanUp(false)
            .Build();

        await image.CreateAsync();
        return image;
    }

    private static async Task<RunningInstance> StartContainerAsync(
        SqlServerImageSpec spec, IFutureDockerImage image, string backupDir)
    {
        // Poll until the AdventureWorksLT restore (run asynchronously by configure-db.sh) completes.
        string waitQuery =
            $"SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = '{spec.DatabaseName}'";
        string waitCmd =
            $"/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$MSSQL_SA_PASSWORD\" -C -h -1 -W " +
            $"-Q \"{waitQuery}\" | grep -q 1";

        IContainer container = new ContainerBuilder()
            .WithImage(image)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithEnvironment("MSSQL_DB_BAK", spec.RestoreScriptPath)
            .WithBindMount(backupDir, "/var/opt/mssql/backup", AccessMode.ReadOnly)
            .WithPortBinding(MsSqlPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandIsCompleted(
                    new[] { "/bin/bash", "-c", waitCmd },
                    o => o.WithTimeout(TimeSpan.FromMinutes(5))))
            .Build();

        await container.StartAsync();

        var csb = new SqlConnectionStringBuilder
        {
            DataSource = $"{container.Hostname},{container.GetMappedPublicPort(MsSqlPort)}",
            UserID = "sa",
            Password = SaPassword,
            TrustServerCertificate = true,
            Encrypt = true,
        };

        return new RunningInstance(container, csb.ConnectionString);
    }

    /// <summary>The connection string for the given running SQL Server version.</summary>
    public string GetConnectionString(SqlServerVersion version) => _instances[version].ConnectionString;

    /// <summary>
    /// Builds a real <see cref="ServerConnectionFactory"/> backed by an
    /// <see cref="IOptionsMonitor{TOptions}"/> carrying the supplied data sources.
    /// </summary>
    internal IServerConnectionFactory CreateFactory(params DataSource[] dataSources)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.Configure<MainConfiguration>(c => c.DataSources = dataSources);

        ServiceProvider provider = services.BuildServiceProvider();
        ILogger<ServerConnectionFactory> logger =
            provider.GetRequiredService<ILogger<ServerConnectionFactory>>();
        IOptionsMonitor<MainConfiguration> monitor =
            provider.GetRequiredService<IOptionsMonitor<MainConfiguration>>();

        return new ServerConnectionFactory(logger, monitor);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (RunningInstance instance in _instances.Values)
        {
            await instance.Container.DisposeAsync();
        }
    }
}
