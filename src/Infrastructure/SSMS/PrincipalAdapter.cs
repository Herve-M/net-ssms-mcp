using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Abstractions.Security;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class PrincipalAdapter(IServerPort serverPort, IDatabasePort databasePort) : IPrincipalPort
{
    public async Task<IReadOnlyCollection<Login>> GetServerLogins(string serverName, CancellationToken cancellationToken)
    {
        Server server = await serverPort.GetServer(serverName, cancellationToken);
        return server.Logins.Cast<Login>().ToList();
    }

    public async Task<IReadOnlyCollection<ServerRole>> GetServerRoles(string serverName, CancellationToken cancellationToken)
    {
        Server server = await serverPort.GetServer(serverName, cancellationToken);
        return server.Roles.Cast<ServerRole>().ToList();
    }

    public async Task<IReadOnlyCollection<User>> GetDatabaseUsers(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Users.Cast<User>().ToList();
    }

    public async Task<IReadOnlyCollection<DatabaseRole>> GetDatabaseRoles(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);
        return database.Roles.Cast<DatabaseRole>().ToList();
    }
}
