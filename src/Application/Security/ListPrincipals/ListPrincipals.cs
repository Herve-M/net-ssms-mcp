using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Security;

namespace ssmsmcp.Application.Security;

public sealed record PrincipalDto
{
    public required string PrincipalKind { get; init; }
    public required string Name { get; init; }
    public required string TypeDesc { get; init; }
    public string? Database { get; init; }
    public int PrincipalId { get; init; }
    public string? Sid { get; init; }
    public string? AuthType { get; init; }
    public bool? IsDisabled { get; init; }
    public string? DefaultDatabase { get; init; }
    public string? DefaultSchema { get; init; }
    public bool? IsFixedRole { get; init; }
    public string? OwningPrincipal { get; init; }
    public DateTime? CreateDate { get; init; }
    public DateTime? ModifyDate { get; init; }
}

public sealed record ListPrincipalsRequest(
    string ServerName,
    string? DatabaseName,
    string Scope,          // SERVER | DATABASE | BOTH
    string PrincipalType,  // LOGIN | USER | SERVER_ROLE | DATABASE_ROLE | ANY
    string? NamePattern,
    bool IncludeSystem,
    PageRequest Pagination) : IRequest<PagedResult<PrincipalDto>>;

public sealed class ListPrincipalsHandler(IPrincipalPort principalPort)
    : IRequestHandler<ListPrincipalsRequest, PagedResult<PrincipalDto>>
{
    private readonly IPrincipalPort _principalPort = principalPort;

    public async ValueTask<PagedResult<PrincipalDto>> Handle(ListPrincipalsRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        bool wantServer = request.Scope is "SERVER" or "BOTH";
        bool wantDatabase = (request.Scope is "DATABASE" or "BOTH") && request.DatabaseName is not null;

        List<PrincipalDto> all = new();

        if (wantServer)
        {
            if (request.PrincipalType is "LOGIN" or "ANY")
            {
                foreach (Login login in await _principalPort.GetServerLogins(request.ServerName, cancellationToken))
                {
                    all.Add(PrincipalMapper.FromLogin(login));
                }
            }

            if (request.PrincipalType is "SERVER_ROLE" or "ANY")
            {
                foreach (ServerRole role in await _principalPort.GetServerRoles(request.ServerName, cancellationToken))
                {
                    all.Add(PrincipalMapper.FromServerRole(role));
                }
            }
        }

        if (wantDatabase)
        {
            string db = request.DatabaseName!;

            if (request.PrincipalType is "USER" or "ANY")
            {
                foreach (User user in await _principalPort.GetDatabaseUsers(request.ServerName, db, cancellationToken))
                {
                    all.Add(PrincipalMapper.FromUser(user, db));
                }
            }

            if (request.PrincipalType is "DATABASE_ROLE" or "ANY")
            {
                foreach (DatabaseRole role in await _principalPort.GetDatabaseRoles(request.ServerName, db, cancellationToken))
                {
                    all.Add(PrincipalMapper.FromDatabaseRole(role, db));
                }
            }
        }

        IEnumerable<PrincipalDto> filtered = all;

        if (!request.IncludeSystem)
        {
            filtered = filtered.Where(p => !PrincipalMapper.IsSystem(p));
        }

        if (!string.IsNullOrEmpty(request.NamePattern))
        {
            filtered = filtered.Where(p => p.Name.Contains(request.NamePattern, StringComparison.OrdinalIgnoreCase));
        }

        List<PrincipalDto> sorted = filtered
            .OrderBy(p => p.Database is null ? 0 : 1)          // SERVER scope first
            .ThenBy(p => p.Database, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.PrincipalKind, StringComparer.Ordinal)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int totalCount = sorted.Count;
        PrincipalDto[] page = sorted
            .Skip(request.Pagination.Skip)
            .Take(request.Pagination.Take)
            .ToArray();

        return PagedResult<PrincipalDto>.Create(page, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}

internal static class PrincipalMapper
{
    public static PrincipalDto FromLogin(Login login) => new()
    {
        PrincipalKind = "SERVER_LOGIN",
        Name = login.Name,
        TypeDesc = login.LoginType.ToString().ToUpperInvariant(),
        Database = null,
        PrincipalId = login.ID,
        Sid = ToHex(login.Sid),
        AuthType = login.LoginType switch
        {
            LoginType.WindowsUser or LoginType.WindowsGroup => "WINDOWS",
            LoginType.SqlLogin => "SQL",
            LoginType.ExternalUser or LoginType.ExternalGroup => "ENTRA_ID",
            LoginType.Certificate => "CERTIFICATE",
            LoginType.AsymmetricKey => "ASYMMETRIC_KEY",
            _ => null,
        },
        IsDisabled = login.IsDisabled,
        DefaultDatabase = login.DefaultDatabase,
        CreateDate = login.CreateDate,
        ModifyDate = login.DateLastModified,
    };

    public static PrincipalDto FromServerRole(ServerRole role) => new()
    {
        PrincipalKind = "SERVER_ROLE",
        Name = role.Name,
        TypeDesc = "SERVER_ROLE",
        Database = null,
        PrincipalId = role.ID,
        IsFixedRole = role.IsFixedRole,
        OwningPrincipal = role.Owner,
    };

    public static PrincipalDto FromUser(User user, string database) => new()
    {
        PrincipalKind = "DATABASE_USER",
        Name = user.Name,
        TypeDesc = user.UserType.ToString().ToUpperInvariant(),
        Database = database,
        PrincipalId = user.ID,
        Sid = ToHex(user.Sid),
        DefaultSchema = user.DefaultSchema,
        CreateDate = user.CreateDate,
        ModifyDate = user.DateLastModified,
    };

    public static PrincipalDto FromDatabaseRole(DatabaseRole role, string database) => new()
    {
        PrincipalKind = "DATABASE_ROLE",
        Name = role.Name,
        TypeDesc = "DATABASE_ROLE",
        Database = database,
        PrincipalId = role.ID,
        IsFixedRole = role.IsFixedRole,
        OwningPrincipal = role.Owner,
        CreateDate = role.CreateDate,
        ModifyDate = role.DateLastModified,
    };

    public static bool IsSystem(PrincipalDto p)
    {
        if (p.IsFixedRole == true)
        {
            return true;
        }

        string n = p.Name;
        return n == "sa"
            || n == "public"
            || n == "dbo"
            || n == "guest"
            || n == "INFORMATION_SCHEMA"
            || n == "sys"
            || (n.StartsWith("##", StringComparison.Ordinal) && n.EndsWith("##", StringComparison.Ordinal))
            || n.StartsWith("NT SERVICE\\", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("NT AUTHORITY\\", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("db_", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ToHex(byte[]? sid) => sid is null || sid.Length == 0 ? null : "0x" + Convert.ToHexString(sid);
}
