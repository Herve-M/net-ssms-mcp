using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IDatabasePort
{
    Task<Database> GetDatabase(string serverName, string name, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken cancellationToken);
}
