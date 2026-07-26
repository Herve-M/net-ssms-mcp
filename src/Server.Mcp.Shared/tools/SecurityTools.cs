using System.ComponentModel;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Application.Security;
using ssmsmcp.Server.Mcp.Shared.Abstractions;
using ssmsmcp.Server.Mcp.tools.Abstractions;

namespace ssmsmcp.Server.Mcp.tools;

internal sealed class SecurityTools(IMediator mediator, IDefaultServerName defaultServerName)
{
    private readonly IMediator _mediator = mediator;
    private readonly IDefaultServerName _defaultServerName = defaultServerName;

    [McpServerTool(
        Name = "list_principals",
        Title = "List Principals",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists security principals — server logins, database users, server roles, and database roles — as a flat, paginated list. Each item carries a principal_kind discriminator (SERVER_LOGIN, DATABASE_USER, SERVER_ROLE, DATABASE_ROLE).")]
    public async Task<CallToolResult> ListPrincipals(
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Target database for DATABASE/BOTH scope. Required to include database users/roles.")]
        string? database = null,
        [Description("Which catalog to query: SERVER, DATABASE, or BOTH. Default BOTH.")]
        string scope = "BOTH",
        [Description("Filter to one kind: LOGIN, USER, SERVER_ROLE, DATABASE_ROLE, or ANY. Default ANY.")]
        string principal_type = "ANY",
        [Description("Case-insensitive substring to filter principal names. Null returns all.")]
        string? name_pattern = null,
        [Description("Include built-in principals (sa, ##MS_*##, NT SERVICE\\*, fixed roles, dbo/guest/public). Default false.")]
        bool include_system = false,
        [Description("Page number (1-based).")]
        int page = 1,
        [Description("Number of items per page (max 100).")]
        int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        PageRequest pagination = new()
        {
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(page_size, 1, 100),
        };

        try
        {
            PagedResult<PrincipalDto> result = await _mediator.Send(
                new ListPrincipalsRequest(
                    resolved,
                    database,
                    scope.ToUpperInvariant(),
                    principal_type.ToUpperInvariant(),
                    name_pattern,
                    include_system,
                    pagination),
                cancellationToken);

            return ToolPayload.Structured(result);
        }
        catch (InvalidOperationException ex)
        {
            // The database-scoped path throws when the target database does not exist (SPEC OBJECT_NOT_FOUND).
            return ToolPayload.NotFound(ex.Message);
        }
    }

    [McpServerTool(
        Name = "list_role_memberships",
        Title = "List Role Memberships",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists direct role -> member edges at server and/or database scope. A non-existent role or member returns an empty list, not an error.")]
    public async Task<CallToolResult> ListRoleMemberships(
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Target database for DATABASE/BOTH scope.")]
        string? database = null,
        [Description("Which catalog to query: SERVER, DATABASE, or BOTH. Default BOTH.")]
        string scope = "BOTH",
        [Description("Filter to memberships of this role name (case-insensitive). Null returns all.")]
        string? role = null,
        [Description("Filter to memberships held by this principal (case-insensitive). Null returns all.")]
        string? member = null,
        [Description("Reserved / not yet applied: intended to expand nested role memberships transitively. Currently only direct edges are returned.")]
        bool include_transitive = false, //TODO: transitive BFS expansion
        [Description("Include memberships involving built-in fixed roles (sysadmin, db_owner, etc.). Default false.")]
        bool include_system = false,
        [Description("Page number (1-based).")]
        int page = 1,
        [Description("Number of items per page (max 100).")]
        int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        PageRequest pagination = new()
        {
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(page_size, 1, 100),
        };

        try
        {
            PagedResult<RoleMembershipDto> result = await _mediator.Send(
                new ListRoleMembershipsRequest(resolved, database, scope.ToUpperInvariant(), role, member, include_system, pagination),
                cancellationToken);

            return ToolPayload.Structured(result);
        }
        catch (InvalidOperationException ex)
        {
            // The database-scoped path throws when the target database does not exist (SPEC OBJECT_NOT_FOUND).
            return ToolPayload.NotFound(ex.Message);
        }
    }

    [McpServerTool(
        Name = "list_permissions",
        Title = "List Permissions",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists directly granted permissions, scoped by principal and/or securable. Returns GRANT/DENY/REVOKE rows. Inherited (role) permissions are not yet expanded.")]
    public async Task<CallToolResult> ListPermissions(
        [Description("Target SQL Server data-source name. Omit to use the default ('main' on the stdio host).")]
        string? server_name = null,
        [Description("Target database for DATABASE/SCHEMA/OBJECT securables.")]
        string? database = null,
        [Description("Filter to a single principal (grantee) name. Null returns all.")]
        string? principal_name = null,
        [Description("Securable class: SERVER, DATABASE, SCHEMA, or OBJECT. Null returns all classes.")]
        string? securable_type = null,
        [Description("Case-insensitive substring of the securable name (FQN) to filter on. Null returns all.")]
        string? securable_name = null,
        [Description("Reserved / not yet applied: intended to include role-inherited permissions for principal_name. Currently only direct grants are returned.")]
        bool include_inherited = true, //TODO: inherited permission union
        [Description("Page number (1-based).")]
        int page = 1,
        [Description("Number of items per page (max 100).")]
        int page_size = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved))
        {
            return ToolPayload.MissingServerName();
        }

        PageRequest pagination = new()
        {
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(page_size, 1, 100),
        };

        try
        {
            PagedResult<PermissionDto> result = await _mediator.Send(
                new ListPermissionsRequest(
                    resolved,
                    database,
                    principal_name,
                    securable_type?.ToUpperInvariant(),
                    securable_name,
                    pagination),
                cancellationToken);

            return ToolPayload.Structured(result);
        }
        catch (InvalidOperationException ex)
        {
            // The database-scoped path throws when the target database does not exist (SPEC OBJECT_NOT_FOUND).
            return ToolPayload.NotFound(ex.Message);
        }
    }
}
