using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class RoleAdapter(IDatabasePort databasePort) : IRolePort
{
    public async Task<IReadOnlyCollection<DatabaseRole>> GetDatabaseRoles(string serverName, string databaseName, int skip, int take, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        return database.Roles
            .Cast<DatabaseRole>()
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public async Task<int> GetDatabaseRolesCount(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Roles.Count;
    }
}