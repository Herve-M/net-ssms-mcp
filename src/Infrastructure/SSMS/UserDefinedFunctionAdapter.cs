using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class UserDefinedFunctionAdapter(IDatabasePort databasePort) : IUserDefinedFunctionPort
{
    public async Task<IReadOnlyCollection<UserDefinedFunction>> GetDatabaseUserDefinedFunctions(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.UserDefinedFunctions
            .Cast<UserDefinedFunction>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseUserDefinedFunctionsCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.UserDefinedFunctions.Count;
    }
}