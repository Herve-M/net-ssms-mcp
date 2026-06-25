using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class UserDefinedTableTypeAdapter(IDatabasePort databasePort) : IUserDefinedTableTypePort
{
    public async Task<IReadOnlyCollection<UserDefinedTableType>> GetDatabaseUserDefinedTableTypes(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.UserDefinedTableTypes
            .Cast<UserDefinedTableType>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseUserDefinedTableTypesCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.UserDefinedTableTypes.Count;
    }
}