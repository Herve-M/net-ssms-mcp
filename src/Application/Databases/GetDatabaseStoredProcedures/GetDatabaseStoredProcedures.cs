using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseStoredProcedureDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseStoredProceduresRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseStoredProcedureDto>>;

public sealed class GetDatabaseStoredProceduresHandler(IStoredProcedurePort storedProcedurePort)
    : IRequestHandler<GetDatabaseStoredProceduresRequest, PagedResult<DatabaseStoredProcedureDto>>
{
    private readonly IStoredProcedurePort _storedProcedurePort = storedProcedurePort;

    public async ValueTask<PagedResult<DatabaseStoredProcedureDto>> Handle(GetDatabaseStoredProceduresRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _storedProcedurePort.GetDatabaseStoredProceduresCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<StoredProcedure> storedProcedures = await _storedProcedurePort.GetDatabaseStoredProcedures(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseStoredProcedureDto[] storedProcedureDtos = storedProcedures
            .Select(storedProcedure => new DatabaseStoredProcedureDto
            {
                Name = storedProcedure.Name
            })
            .ToArray();

        return PagedResult<DatabaseStoredProcedureDto>.Create(storedProcedureDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}