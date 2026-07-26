using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Security;

public interface IPrincipalPort
{
    Task<IReadOnlyCollection<Login>> GetServerLogins(string serverName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ServerRole>> GetServerRoles(string serverName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<User>> GetDatabaseUsers(string serverName, string databaseName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DatabaseRole>> GetDatabaseRoles(string serverName, string databaseName, CancellationToken cancellationToken);
}
