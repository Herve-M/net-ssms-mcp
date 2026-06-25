using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class TableAdapter(IDatabasePort databasePort) : ITablePort
{
    public async Task<IReadOnlyCollection<Table>> GetDatabaseTables(
        string serverName,
        string databaseName,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.Tables
            .Cast<Table>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseTablesCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Tables.Count;
    }
}