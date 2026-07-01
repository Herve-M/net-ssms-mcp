using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseUserDefinedTableTypeDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseUserDefinedTableTypesRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseUserDefinedTableTypeDto>>;

public sealed class GetDatabaseUserDefinedTableTypesHandler(IUserDefinedTableTypePort userDefinedTableTypePort)
    : IRequestHandler<GetDatabaseUserDefinedTableTypesRequest, PagedResult<DatabaseUserDefinedTableTypeDto>>
{
    private readonly IUserDefinedTableTypePort _userDefinedTableTypePort = userDefinedTableTypePort;

    public async ValueTask<PagedResult<DatabaseUserDefinedTableTypeDto>> Handle(GetDatabaseUserDefinedTableTypesRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _userDefinedTableTypePort.GetDatabaseUserDefinedTableTypesCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<UserDefinedTableType> userDefinedTableTypes = await _userDefinedTableTypePort.GetDatabaseUserDefinedTableTypes(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseUserDefinedTableTypeDto[] userDefinedTableTypeDtos = userDefinedTableTypes
            .Select(userDefinedTableType => new DatabaseUserDefinedTableTypeDto
            {
                Name = userDefinedTableType.Name
            })
            .ToArray();

        return PagedResult<DatabaseUserDefinedTableTypeDto>.Create(userDefinedTableTypeDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}