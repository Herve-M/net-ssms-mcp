using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ssmsmcp.Domain.Abstractions.Security;

public sealed record RoleMembershipEdge(
    string Scope,          // SERVER | DATABASE
    string? Database,
    string Role,
    string RoleTypeDesc,   // SERVER_ROLE | DATABASE_ROLE
    string Member,
    string MemberTypeDesc);

public interface IRoleMembershipPort
{
    Task<IReadOnlyCollection<RoleMembershipEdge>> GetServerRoleMemberships(string serverName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleMembershipEdge>> GetDatabaseRoleMemberships(string serverName, string databaseName, CancellationToken cancellationToken);
}
