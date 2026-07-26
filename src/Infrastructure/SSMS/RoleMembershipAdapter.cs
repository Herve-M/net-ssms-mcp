using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Abstractions.Security;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class RoleMembershipAdapter(IServerPort serverPort, IDatabasePort databasePort) : IRoleMembershipPort
{
    public async Task<IReadOnlyCollection<RoleMembershipRecord>> GetServerRoleMemberships(string serverName, CancellationToken cancellationToken)
    {
        Server server = await serverPort.GetServer(serverName, cancellationToken);

        Dictionary<string, string> loginTypes = server.Logins
            .Cast<Login>()
            .ToDictionary(l => l.Name, l => l.LoginType.ToString().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> roleNames = server.Roles
            .Cast<ServerRole>()
            .ToDictionary(r => r.Name, _ => "SERVER_ROLE", StringComparer.OrdinalIgnoreCase);

        List<RoleMembershipRecord> edges = new();
        foreach (ServerRole role in server.Roles.Cast<ServerRole>())
        {
            foreach (string member in role.EnumMemberNames().Cast<string>())
            {
                string memberType = roleNames.TryGetValue(member, out string? rt) ? rt
                    : loginTypes.TryGetValue(member, out string? lt) ? lt
                    : "UNKNOWN";
                edges.Add(new RoleMembershipRecord("SERVER", null, role.Name, "SERVER_ROLE", member, memberType));
            }
        }

        return edges;
    }

    public async Task<IReadOnlyCollection<RoleMembershipRecord>> GetDatabaseRoleMemberships(string serverName, string databaseName, CancellationToken cancellationToken)
    {
        Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

        Dictionary<string, string> userTypes = database.Users
            .Cast<User>()
            .ToDictionary(u => u.Name, u => u.UserType.ToString().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> roleNames = database.Roles
            .Cast<DatabaseRole>()
            .ToDictionary(r => r.Name, _ => "DATABASE_ROLE", StringComparer.OrdinalIgnoreCase);

        List<RoleMembershipRecord> edges = new();
        foreach (DatabaseRole role in database.Roles.Cast<DatabaseRole>())
        {
            foreach (string member in role.EnumMembers().Cast<string>())
            {
                string memberType = roleNames.TryGetValue(member, out string? rt) ? rt
                    : userTypes.TryGetValue(member, out string? ut) ? ut
                    : "UNKNOWN";
                edges.Add(new RoleMembershipRecord("DATABASE", databaseName, role.Name, "DATABASE_ROLE", member, memberType));
            }
        }

        return edges;
    }
}
