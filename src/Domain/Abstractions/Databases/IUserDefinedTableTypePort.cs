using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IUserDefinedTableTypePort
{
    Task<IReadOnlyCollection<UserDefinedTableType>> GetDatabaseUserDefinedTableTypes(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseUserDefinedTableTypesCount(string serverName, string databaseName, CancellationToken cancellationToken);
}