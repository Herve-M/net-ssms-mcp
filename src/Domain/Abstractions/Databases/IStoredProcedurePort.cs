using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IStoredProcedurePort
{
    Task<IReadOnlyCollection<StoredProcedure>> GetDatabaseStoredProcedures(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseStoredProceduresCount(string serverName, string databaseName, CancellationToken cancellationToken);
}