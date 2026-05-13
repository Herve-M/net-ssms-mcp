using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Configurations;
using ssmsmcp.Domain.Configurations;
using ssmsmcp.Infrastructure.Abstractions.SSMS;

namespace ssmsmcp.Infrastructure.SSMS.Internals;

internal sealed class ServerConnectionFactory(ILogger<ServerConnectionFactory> logger,
    IOptionsMonitor<MainConfiguration> optionsMonitor)
        : IServerConnectionFactory
{
    private readonly ILogger<ServerConnectionFactory> _logger = logger;
    private readonly IOptionsMonitor<MainConfiguration> _optionsMonitor = optionsMonitor;

    private IMainConfiguration _mainConfiguration => _optionsMonitor.CurrentValue;

    private readonly ConcurrentDictionary<string, ServerConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Server> _servers = new(StringComparer.OrdinalIgnoreCase);

    public void Dispose()
    {
        foreach ((string _, ServerConnection connection) in _connections)
        {
            connection.ForceDisconnected();
        }
    }

    public ValueTask<Server> ConnectTo(in string connectionString)
    {
        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

        if (_connections.TryGetValue(builder.DataSource, out ServerConnection value))
        {
            return new ValueTask<Server>(new Server(value));
        }

        try
        {
            ServerConnection serverConnection = new ServerConnection(new SqlConnection(builder.ConnectionString));
            Server server = new Server(serverConnection);

            _connections.TryAdd(builder.DataSource, serverConnection);
            _servers.TryAdd(builder.DataSource, server);
            return new ValueTask<Server>(server);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to connect to server with {ConnectionString}, see:", connectionString.ToString());
            return new ValueTask<Server>(Task.FromException<Server>(e));
        }
    }

    public ValueTask<Server> GetServer(in string serverName)
    {
        string s = serverName;
        if (_mainConfiguration.DataSources.Any(x => x.Name == s))
        {
            DataSource config = _mainConfiguration.DataSources.First(x => x.Name == s);

            return ConnectTo(config.ConnectionString);
        }

        return new ValueTask<Server>(
            Task.FromException<Server>(
                new InvalidOperationException($"Server '{serverName}' not found in configuration")));
    }

    public ValueTask<IReadOnlyCollection<Server>> GetAllServers()
    {
        return new ValueTask<IReadOnlyCollection<Server>>(_servers.Values.ToArray());
    }
}
