using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class UserAdapter(IDatabasePort databasePort) : IUserPort
{
    public async Task<IReadOnlyCollection<User>> GetDatabaseUsers(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.Users
            .Cast<User>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseUsersCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Users.Count;
    }
}