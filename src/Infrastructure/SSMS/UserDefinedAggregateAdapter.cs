using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class UserDefinedAggregateAdapter(IDatabasePort databasePort) : IUserDefinedAggregatePort
{
    public async Task<UserDefinedAggregate?> GetUserDefinedAggregate(string serverName, string databaseName, string schema, string name, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.UserDefinedAggregates[name, schema];
    }
}
