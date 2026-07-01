using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IViewPort
{
    Task<IReadOnlyCollection<View>> GetDatabaseViews(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken);

    Task<int> GetDatabaseViewsCount(string serverName, string databaseName, CancellationToken cancellationToken);
}