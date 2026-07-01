using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseUserDefinedTypeDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseUserDefinedTypesRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseUserDefinedTypeDto>>;

public sealed class GetDatabaseUserDefinedTypesHandler(IUserDefinedTypePort userDefinedTypePort)
    : IRequestHandler<GetDatabaseUserDefinedTypesRequest, PagedResult<DatabaseUserDefinedTypeDto>>
{
    private readonly IUserDefinedTypePort _userDefinedTypePort = userDefinedTypePort;

    public async ValueTask<PagedResult<DatabaseUserDefinedTypeDto>> Handle(GetDatabaseUserDefinedTypesRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _userDefinedTypePort.GetDatabaseUserDefinedTypesCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<UserDefinedDataType> userDefinedTypes = await _userDefinedTypePort.GetDatabaseUserDefinedTypes(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseUserDefinedTypeDto[] userDefinedTypeDtos = userDefinedTypes
            .Select(userDefinedType => new DatabaseUserDefinedTypeDto
            {
                Name = userDefinedType.Name
            })
            .ToArray();

        return PagedResult<DatabaseUserDefinedTypeDto>.Create(userDefinedTypeDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}