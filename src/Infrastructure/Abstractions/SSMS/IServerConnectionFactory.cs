using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Infrastructure.Abstractions.SSMS;

internal interface IServerConnectionFactory
    : IDisposable
{
    ValueTask<Server> ConnectTo(in string connectionString);
    ValueTask<Server> GetServer(in string serverName, in bool forceNewConnection = false);
    ValueTask<IReadOnlyCollection<Server>> GetAllServers();
}
