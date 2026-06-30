using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IDatabasePort
{
    Task<Database> GetDatabase(string serverName, string name, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Database>> GetDatabases(string serverName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Schema>> GetDatabaseSchemas(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseSchemasCount(string serverName, string databaseName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DatabaseObjectInfo>> GetDatabaseObjects(string serverName, string databaseName, DatabaseObjectTypes types, bool forceRefresh, CancellationToken cancellationToken);
}
