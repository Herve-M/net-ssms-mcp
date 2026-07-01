using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class UserDefinedTypeAdapter(IDatabasePort databasePort) : IUserDefinedTypePort
{
    public async Task<IReadOnlyCollection<UserDefinedDataType>> GetDatabaseUserDefinedTypes(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.UserDefinedDataTypes
            .Cast<UserDefinedDataType>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseUserDefinedTypesCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.UserDefinedDataTypes.Count;
    }
}