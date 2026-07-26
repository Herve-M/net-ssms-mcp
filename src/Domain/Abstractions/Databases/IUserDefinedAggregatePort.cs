using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace ssmsmcp.Domain.Abstractions.Databases;

public interface IUserDefinedAggregatePort
{
    Task<UserDefinedAggregate?> GetUserDefinedAggregate(string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken);
}
