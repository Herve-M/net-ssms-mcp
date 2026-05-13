using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Servers;

public interface IServerPort
{
    Task<Server> GetServer(string name, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Server>> GetServers(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken cancellationToken);
}
