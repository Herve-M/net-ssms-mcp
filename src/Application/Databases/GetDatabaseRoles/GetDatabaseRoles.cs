using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseRoleDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseRolesRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseRoleDto>>;

public sealed class GetDatabaseRolesHandler(IRolePort rolePort)
    : IRequestHandler<GetDatabaseRolesRequest, PagedResult<DatabaseRoleDto>>
{
    private readonly IRolePort _rolePort = rolePort;

    public async ValueTask<PagedResult<DatabaseRoleDto>> Handle(GetDatabaseRolesRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _rolePort.GetDatabaseRolesCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<DatabaseRole> roles = await _rolePort.GetDatabaseRoles(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseRoleDto[] roleDtos = roles
            .Select(role => new DatabaseRoleDto
            {
                Name = role.Name
            })
            .ToArray();

        return PagedResult<DatabaseRoleDto>.Create(roleDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}