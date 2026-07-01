using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseTriggerDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseTriggersRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseTriggerDto>>;

public sealed class GetDatabaseTriggersHandler(ITriggerPort triggerPort)
    : IRequestHandler<GetDatabaseTriggersRequest, PagedResult<DatabaseTriggerDto>>
{
    private readonly ITriggerPort _triggerPort = triggerPort;

    public async ValueTask<PagedResult<DatabaseTriggerDto>> Handle(GetDatabaseTriggersRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _triggerPort.GetDatabaseTriggersCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<DatabaseDdlTrigger> triggers = await _triggerPort.GetDatabaseTriggers(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseTriggerDto[] triggerDtos = triggers
            .Select(trigger => new DatabaseTriggerDto
            {
                Name = trigger.Name
            })
            .ToArray();

        return PagedResult<DatabaseTriggerDto>.Create(triggerDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}