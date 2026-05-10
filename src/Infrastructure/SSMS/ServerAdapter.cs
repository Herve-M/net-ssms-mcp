using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;
using ssmsmcp.Infrastructure.Abstractions.SSMS;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class ServerAdapter(IServerConnectionFactory factory)
    : IServerPort
{
    public async Task<Server> GetServer(string name, CancellationToken cancellationToken)
    {
        return await factory.GetServer(name);
    }

    public Task<IReadOnlyCollection<Server>> GetServers(CancellationToken cancellationToken)
    {
        return factory.GetAllServers().AsTask();
    }

    public async Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken cancellationToken)
    {
        var server = await factory.GetServer(serverName);
        return server.Databases.Select(x => x).ToList();
    }
}
