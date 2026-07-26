using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;
using ssmsmcp.Domain.Abstractions.Security;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Infrastructure.SSMS;

internal sealed class PermissionAdapter(IServerPort serverPort, IDatabasePort databasePort) : IPermissionPort
{
    public async Task<IReadOnlyCollection<PermissionRecord>> GetPermissions(
        string serverName, string? databaseName, string? securableType, CancellationToken cancellationToken)
    {
        List<PermissionRecord> records = new();

        bool wantServer = securableType is null or "SERVER";
        bool wantDatabase = securableType is null or "DATABASE" or "SCHEMA";
        bool wantObject = securableType is null or "OBJECT";

        if (wantServer)
        {
            Server server = await serverPort.GetServer(serverName, cancellationToken);
            foreach (ServerPermissionInfo info in server.EnumServerPermissions())
            {
                records.AddRange(Expand(info, info.PermissionType.ToString(), "SERVER", server.Name, securableSchema: null));
            }
        }

        if (wantDatabase || wantObject)
        {
            if (databaseName is null)
            {
                return records;
            }

            Database database = await databasePort.GetDatabase(serverName, databaseName, cancellationToken);

            if (wantDatabase)
            {
                foreach (DatabasePermissionInfo info in database.EnumDatabasePermissions())
                {
                    records.AddRange(Expand(info, info.PermissionType.ToString(), "DATABASE", database.Name, securableSchema: null));
                }
            }

            if (wantObject)
            {
                foreach (ObjectPermissionInfo info in database.EnumObjectPermissions())
                {
                    // Pass the raw object schema/name; the Application handler qualifies and quotes the display name.
                    string? schema = string.IsNullOrEmpty(info.ObjectSchema) ? null : info.ObjectSchema;
                    records.AddRange(Expand(info, info.PermissionType.ToString(), "OBJECT", info.ObjectName, schema));
                }
            }
        }

        return records;
    }

    private static IEnumerable<PermissionRecord> Expand(PermissionInfo info, string permissionTypeText, string securableType, string securable, string? securableSchema)
    {
        string state = info.PermissionState switch
        {
            PermissionState.Grant or PermissionState.GrantWithGrant => "GRANT",
            PermissionState.Deny => "DENY",
            PermissionState.Revoke => "REVOKE",
            _ => info.PermissionState.ToString().ToUpperInvariant(),
        };

        foreach (string permission in SplitPermissionNames(permissionTypeText))
        {
            yield return new PermissionRecord(
                info.Grantee,
                info.GranteeType.ToString().ToUpperInvariant(),
                permission,
                state,
                securable,
                securableType,
                info.Grantor,
                securableSchema);
        }
    }

    private static IEnumerable<string> SplitPermissionNames(string permissionType) =>
        permissionType
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .DefaultIfEmpty(permissionType.Trim());
}
