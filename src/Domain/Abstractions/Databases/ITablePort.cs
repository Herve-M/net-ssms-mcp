using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface ITablePort
{
    Task<IReadOnlyCollection<Table>> GetDatabaseTables(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseTablesCount(string serverName, string databaseName, CancellationToken cancellationToken);
}