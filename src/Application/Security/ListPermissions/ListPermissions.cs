using Mediator;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Security;

namespace ssmsmcp.Application.Security;

public sealed record PermissionDto
{
    public required string Principal { get; init; }
    public required string PrincipalType { get; init; }
    public required string PermissionName { get; init; }
    public required string State { get; init; }
    public required string Securable { get; init; }
    public required string SecurableType { get; init; }
    public required string Grantor { get; init; }
    public bool IsInherited { get; init; }
    public string? InheritedViaRole { get; init; }
}

public sealed record ListPermissionsRequest(
    string ServerName,
    string? DatabaseName,
    string? PrincipalName,
    string? SecurableType,
    string? SecurableName,
    PageRequest Pagination) : IRequest<PagedResult<PermissionDto>>;

public sealed class ListPermissionsHandler(IPermissionPort permissionPort)
    : IRequestHandler<ListPermissionsRequest, PagedResult<PermissionDto>>
{
    private readonly IPermissionPort _permissionPort = permissionPort;

    public async ValueTask<PagedResult<PermissionDto>> Handle(ListPermissionsRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        IReadOnlyCollection<PermissionRecord> records = await _permissionPort.GetPermissions(
            request.ServerName, request.DatabaseName, request.SecurableType, cancellationToken);

        IEnumerable<PermissionRecord> filtered = records;

        if (!string.IsNullOrEmpty(request.PrincipalName))
        {
            filtered = filtered.Where(r => string.Equals(r.Principal, request.PrincipalName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.SecurableName))
        {
            filtered = filtered.Where(r => r.Securable.Contains(request.SecurableName, StringComparison.OrdinalIgnoreCase));
        }

        List<PermissionDto> sorted = filtered
            .Select(r => new PermissionDto
            {
                Principal = r.Principal,
                PrincipalType = r.PrincipalType,
                PermissionName = r.PermissionName,
                State = r.State,
                Securable = r.Securable,
                SecurableType = r.SecurableType,
                Grantor = r.Grantor,
                IsInherited = false,
                InheritedViaRole = null,
            })
            .OrderBy(r => r.Principal, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.SecurableType, StringComparer.Ordinal)
            .ThenBy(r => r.Securable, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.PermissionName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int totalCount = sorted.Count;
        PermissionDto[] page = sorted
            .Skip(request.Pagination.Skip)
            .Take(request.Pagination.Take)
            .ToArray();

        return PagedResult<PermissionDto>.Create(page, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}
