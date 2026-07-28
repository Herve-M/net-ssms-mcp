using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ssmsmcp.Domain.Abstractions.Security;

public sealed record RoleMembershipRecord(
    string Scope,          // SERVER | DATABASE
    string? Database,
    string Role,
    string RoleTypeDesc,   // SERVER_ROLE | DATABASE_ROLE
    string Member,
    string MemberTypeDesc);

public interface IRoleMembershipPort
{
    Task<IReadOnlyCollection<RoleMembershipRecord>> GetServerRoleMemberships(string serverName, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleMembershipRecord>> GetDatabaseRoleMemberships(string serverName, string databaseName, CancellationToken cancellationToken);
}
