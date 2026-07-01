using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IRolePort
{
    Task<IReadOnlyCollection<DatabaseRole>> GetDatabaseRoles(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseRolesCount(string serverName, string databaseName, CancellationToken cancellationToken);
}