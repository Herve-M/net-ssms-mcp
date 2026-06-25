using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace ssms.Server.Api.Integration.Tests.Fixtures;

public class AspireContext
    : IAsyncLifetime
{
    // SQL Server data-source names (the `serverName` tool argument), as defined in
    // src/Server.Api/configs/main.json — distinct from the Aspire resource ids (sql-2022/sql-2025).
    public const string Sql2022ServerName = "2022";
    public const string Sql2025ServerName = "2025";

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _startGate = new(1, 1);
    private bool _started;

    private readonly SemaphoreSlim _apiGate = new(1, 1);
    private bool _apiReady;

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

        //TODO: customize?
        // appHost.Services.AddLogging(logging =>
        // {
        //     logging.SetMinimumLevel(LogLevel.Debug);
        //     // Override the logging filters from the app's configuration
        //     logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
        //     logging.AddFilter("Aspire.", LogLevel.Debug);
        //     // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        // });

        Context = await appHost.BuildAsync()
            .WaitAsync(DefaultTimeout, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        _startGate.Dispose();
        _apiGate.Dispose();
    }

    /// <summary>
    /// Starts the distributed application once. Safe to call from multiple tests sharing the fixture.
    /// </summary>
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

    /// <summary>
    /// Ensures the host is started and the (explicit-start) "http-api" resource is started and
    /// healthy (with its dependencies). Idempotent and safe under concurrent callers — the start
    /// command fires exactly once.
    /// </summary>
    private async Task EnsureApiReadyAsync(CancellationToken cancellationToken)
    {
        if (_apiReady)
        {
            return;
        }

        await _apiGate.WaitAsync(cancellationToken);
        try
        {
            if (!_apiReady)
            {
                await EnsureStartedAsync(cancellationToken);

                await Context.ResourceCommands.ExecuteCommandAsync("http-api", KnownResourceCommands.StartCommand, cancellationToken);

                ResourceEvent apiResource = await Context.ResourceNotifications
                    .WaitForResourceHealthyAsync("http-api", cancellationToken);
                await Context.ResourceNotifications
                    .WaitForDependenciesAsync(apiResource.Resource, cancellationToken);

                _apiReady = true;
            }
        }
        finally
        {
            _apiGate.Release();
        }
    }

    public async Task<HttpClient> GetHttpClientWhenReadyAsync(CancellationToken cancellationToken)
    {
        await EnsureApiReadyAsync(cancellationToken);

        return Context.CreateHttpClient("http-api");
    }

    public async Task<McpClient> GetMcpClientWhenReadyAsync(CancellationToken cancellationToken)
    {
        await EnsureApiReadyAsync(cancellationToken);

        var mcpHttpClient = Context.CreateHttpClient("http-api");
        var httpClientTransport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{mcpHttpClient.BaseAddress}mcp"),
            TransportMode = HttpTransportMode.AutoDetect,
        }, mcpHttpClient);

        return await McpClient.CreateAsync(httpClientTransport, cancellationToken: cancellationToken);
    }
}
