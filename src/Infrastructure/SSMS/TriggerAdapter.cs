using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class TriggerAdapter(IDatabasePort databasePort) : ITriggerPort
{
    public async Task<IReadOnlyCollection<DatabaseDdlTrigger>> GetDatabaseTriggers(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.Triggers
            .Cast<DatabaseDdlTrigger>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseTriggersCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Triggers.Count;
    }
}