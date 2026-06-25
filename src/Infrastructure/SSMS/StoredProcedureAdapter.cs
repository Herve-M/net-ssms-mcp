using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class StoredProcedureAdapter(IDatabasePort databasePort) : IStoredProcedurePort
{
    public async Task<IReadOnlyCollection<StoredProcedure>> GetDatabaseStoredProcedures(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.StoredProcedures
            .Cast<StoredProcedure>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseStoredProceduresCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.StoredProcedures.Count;
    }
}