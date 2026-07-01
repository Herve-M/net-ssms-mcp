using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseViewDto
{
    public required string Name { get; init; }
}

public sealed record GetDatabaseViewsRequest(string ServerName, string DatabaseName, PageRequest Pagination)
    : IRequest<PagedResult<DatabaseViewDto>>;

public sealed class GetDatabaseViewsHandler(IViewPort viewPort)
    : IRequestHandler<GetDatabaseViewsRequest, PagedResult<DatabaseViewDto>>
{
    private readonly IViewPort _viewPort = viewPort;

    public async ValueTask<PagedResult<DatabaseViewDto>> Handle(GetDatabaseViewsRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        int totalCount = await _viewPort.GetDatabaseViewsCount(request.ServerName, request.DatabaseName, cancellationToken);

        IReadOnlyCollection<View> views = await _viewPort.GetDatabaseViews(
            request.ServerName,
            request.DatabaseName,
            request.Pagination.Skip,
            request.Pagination.Take,
            cancellationToken);

        DatabaseViewDto[] viewDtos = views
            .Select(view => new DatabaseViewDto
            {
                Name = view.Name
            })
            .ToArray();

        return PagedResult<DatabaseViewDto>.Create(viewDtos, totalCount, request.Pagination.Page, request.Pagination.PageSize);
    }
}