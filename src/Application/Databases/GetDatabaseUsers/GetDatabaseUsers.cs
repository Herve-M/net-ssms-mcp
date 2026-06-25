using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseUserDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseUsersRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseUserDto>>;

public sealed class GetDatabaseUsersHandler(IUserPort userPort)
    : IRequestHandler<GetDatabaseUsersRequest, PagedResult<DatabaseUserDto>>
{
    private readonly IUserPort _userPort = userPort;

    public async ValueTask<PagedResult<DatabaseUserDto>> Handle(GetDatabaseUsersRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _userPort.GetDatabaseUsersCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<User> users = await _userPort.GetDatabaseUsers(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseUserDto[] userDtos = users
            .Select(user => new DatabaseUserDto
            {
                Name = user.Name
            })
            .ToArray();

        return PagedResult<DatabaseUserDto>.Create(userDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}