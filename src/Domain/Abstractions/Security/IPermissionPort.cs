using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ssmsmcp.Domain.Abstractions.Security;

public sealed record PermissionRecord(
    string Principal,
    string PrincipalType,
    string PermissionName,
    string State,          // GRANT | DENY | REVOKE
    string Securable,
    string SecurableType,  // SERVER | DATABASE | SCHEMA | OBJECT
    string Grantor);

public interface IPermissionPort
{
    Task<IReadOnlyCollection<PermissionRecord>> GetPermissions(
        string serverName,
        string? databaseName,
        string? securableType,   // SERVER | DATABASE | SCHEMA | OBJECT | null (=all)
        CancellationToken cancellationToken);
}
