using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using ModelContextProtocol.Client;

namespace ssmsmcp.Server.Mcp.Integration.Fixtures;

public class AspireContext
    : IAsyncLifetime
{
    public const string Sql2022Resource = "sql-2022";
    public const string Sql2025Resource = "sql-2025";

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _startGate = new(1, 1);
    private bool _started;

    public DistributedApplication Context { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
           .CreateAsync<Projects.Server_Aspire_Host>(
                   args: [
                       "DcpPublisher:RandomizePorts=false"
                   ],
                   configureBuilder: (appOptions, hostSettings) =>
                   {
                       // appOptions.AllowUnsecuredTransport = true;
                       // appOptions.DisableDashboard = false;
                   },
                   TestContext.Current.CancellationToken
               );

        Context = await appHost.BuildAsync()
            .WaitAsync(DefaultTimeout, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        _startGate.Dispose();
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        await _startGate.WaitAsync(cancellationToken);
        try
        {
            if (!_started)
            {
                await Context.StartAsync(cancellationToken);
                _started = true;
            }
        }
        finally
        {
            _startGate.Release();
        }
    }

    public async Task WaitForSqlAsync(string sqlResource, CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken);
        await Context.ResourceNotifications.WaitForResourceHealthyAsync(sqlResource, cancellationToken);
    }

    public async Task<McpClient> GetStdioMcpClientAsync(string sqlResource, CancellationToken cancellationToken)
    {
        await WaitForSqlAsync(sqlResource, cancellationToken);

        string connectionString = await Context.GetConnectionStringAsync(sqlResource, cancellationToken)
            ?? throw new InvalidOperationException($"No connection string available for resource '{sqlResource}'.");

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = $"ssms-mcp-{sqlResource}",
            Command = "dotnet",
            Arguments = ["run", "--project", ResolveServerMcpProjectPath(), "--", "-s", connectionString],
        });

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    private static string ResolveServerMcpProjectPath()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ssms-mcp.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not locate the repository root (ssms-mcp.slnx) from the test base directory.");
        }

        return Path.Combine(dir.FullName, "src", "Server.Mcp", "Server.Mcp.csproj");
    }
}
