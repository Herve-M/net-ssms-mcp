using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseTableDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseTablesRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseTableDto>>;

public sealed class GetDatabaseTablesHandler(ITablePort tablePort)
    : IRequestHandler<GetDatabaseTablesRequest, PagedResult<DatabaseTableDto>>
{
    private readonly ITablePort _tablePort = tablePort;

    public async ValueTask<PagedResult<DatabaseTableDto>> Handle(GetDatabaseTablesRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _tablePort.GetDatabaseTablesCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<Table> tables = await _tablePort.GetDatabaseTables(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseTableDto[] tableDtos = tables
            .Select(table => new DatabaseTableDto
            {
                Name = table.Name
            })
            .ToArray();

        return PagedResult<DatabaseTableDto>.Create(
            tableDtos,
            totalCount,
            request.Pagination.Page,
            request.Pagination.PageSize);
    }
}