using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Infrastructure.Abstractions.SSMS;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class DatabaseAdapter(IServerConnectionFactory factory)
    : IDatabasePort
{
    public async Task<Database> GetDatabase(string serverName, string name, CancellationToken cancellationToken)
    {
        Server server = await factory.GetServer(serverName);
        return server.Databases[name];
    }

    public async Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken cancellationToken)
    {
        Server server = await factory.GetServer(serverName);
        return server.Databases.Select(x => x).ToList();
    }
}
