using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseUserDefinedFunctionDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseUserDefinedFunctionsRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseUserDefinedFunctionDto>>;

public sealed class GetDatabaseUserDefinedFunctionsHandler(IUserDefinedFunctionPort userDefinedFunctionPort)
    : IRequestHandler<GetDatabaseUserDefinedFunctionsRequest, PagedResult<DatabaseUserDefinedFunctionDto>>
{
    private readonly IUserDefinedFunctionPort _userDefinedFunctionPort = userDefinedFunctionPort;

    public async ValueTask<PagedResult<DatabaseUserDefinedFunctionDto>> Handle(GetDatabaseUserDefinedFunctionsRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _userDefinedFunctionPort.GetDatabaseUserDefinedFunctionsCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<UserDefinedFunction> userDefinedFunctions = await _userDefinedFunctionPort.GetDatabaseUserDefinedFunctions(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseUserDefinedFunctionDto[] userDefinedFunctionDtos = userDefinedFunctions
            .Select(userDefinedFunction => new DatabaseUserDefinedFunctionDto
            {
                Name = userDefinedFunction.Name
            })
            .ToArray();

        return PagedResult<DatabaseUserDefinedFunctionDto>.Create(userDefinedFunctionDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}