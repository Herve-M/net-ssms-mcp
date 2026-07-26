using Mediator;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Security;

namespace ssmsmcp.Application.Security;

public sealed record RoleMembershipDto
{
    public required string Scope { get; init; }
    public string? Database { get; init; }
    public required string Role { get; init; }
    public required string RoleTypeDesc { get; init; }
    public required string Member { get; init; }
    public required string MemberTypeDesc { get; init; }
    public bool IsInherited { get; init; }
    public IReadOnlyCollection<string>? InheritedVia { get; init; }
}

public sealed record ListRoleMembershipsRequest(
    string ServerName,
    string? DatabaseName,
    string Scope,          // SERVER | DATABASE | BOTH
    string? Role,
    string? Member,
    bool IncludeSystem,
    PageRequest Pagination) : IRequest<PagedResult<RoleMembershipDto>>;

public sealed class ListRoleMembershipsHandler(IRoleMembershipPort membershipPort)
    : IRequestHandler<ListRoleMembershipsRequest, PagedResult<RoleMembershipDto>>
{
    private static readonly HashSet<string> FixedServerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "sysadmin", "securityadmin", "serveradmin", "setupadmin", "processadmin",
        "diskadmin", "dbcreator", "bulkadmin", "public",
    };

    private readonly IRoleMembershipPort _membershipPort = membershipPort;

    public async ValueTask<PagedResult<RoleMembershipDto>> Handle(ListRoleMembershipsRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        List<RoleMembershipRecord> edges = new();

        if (request.Scope is "SERVER" or "BOTH")
        {
            edges.AddRange(await _membershipPort.GetServerRoleMemberships(request.ServerName, cancellationToken));
        }

        if ((request.Scope is "DATABASE" or "BOTH") && request.DatabaseName is not null)
        {
            edges.AddRange(await _membershipPort.GetDatabaseRoleMemberships(request.ServerName, request.DatabaseName, cancellationToken));
        }

        IEnumerable<RoleMembershipRecord> filtered = edges;

        if (!request.IncludeSystem)
        {
            filtered = filtered.Where(e => !(e.Scope == "SERVER" && FixedServerRoles.Contains(e.Role))
                                        && !(e.Scope == "DATABASE" && e.Role.StartsWith("db_", StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            filtered = filtered.Where(e => string.Equals(e.Role, request.Role, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.Member))
        {
            filtered = filtered.Where(e => string.Equals(e.Member, request.Member, StringComparison.OrdinalIgnoreCase));
        }

        List<RoleMembershipDto> sorted = filtered
            .Select(e => new RoleMembershipDto
            {
                Scope = e.Scope,
                Database = e.Database,
                Role = e.Role,
                RoleTypeDesc = e.RoleTypeDesc,
                Member = e.Member,
                MemberTypeDesc = e.MemberTypeDesc,
                IsInherited = false,
                InheritedVia = null,
            })
            .OrderBy(e => e.Scope, StringComparer.Ordinal)
            .ThenBy(e => e.Database, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Role, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Member, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int totalCount = sorted.Count;
        RoleMembershipDto[] page = sorted
            .Skip(request.Pagination.Skip)
            .Take(request.Pagination.Take)
            .ToArray();

        return PagedResult<RoleMembershipDto>.Create(page, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}
