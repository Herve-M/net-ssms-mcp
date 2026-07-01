using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Application.Abstractions.Shared;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerDatabaseListItemDto
{
    public Guid Id { get; init; }
    public string DatabaseName { get; init; }
}

public sealed record GetServerDatabasesRequest(
    string ServerName,
    string? NamePattern,
    bool IncludeSystem,
    PageRequest Pagination)
    : IRequest<PagedResult<ServerDatabaseListItemDto>>;

public sealed class GetServerDatabasesHandler(ILogger<GetServerDatabasesHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerDatabasesRequest, PagedResult<ServerDatabaseListItemDto>>
{
    private readonly ILogger<GetServerDatabasesHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<PagedResult<ServerDatabaseListItemDto>> Handle(GetServerDatabasesRequest request, CancellationToken cancellationToken)
    {
        request.Pagination.Validate();

        _logger.LogDebug("Getting databases for server {ServerName}", request.ServerName);
        IReadOnlyCollection<Database> databases = await _serverPort.GetDatabases(request.ServerName, cancellationToken);

        IEnumerable<ServerDatabaseListItemDto> query = databases
            .Select(database => new ServerDatabaseListItemDto
            {
                Id = database.DatabaseGuid,
                DatabaseName = database.Name
            });

        if (!string.IsNullOrWhiteSpace(request.NamePattern))
        {
            query = query.Where(d => d.DatabaseName.Contains(request.NamePattern, StringComparison.OrdinalIgnoreCase));
        }

        ServerDatabaseListItemDto[] filtered = query
            .OrderBy(d => d.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ServerDatabaseListItemDto[] pageItems = filtered
            .Skip(request.Pagination.Skip)
            .Take(request.Pagination.Take)
            .ToArray();

        return PagedResult<ServerDatabaseListItemDto>.Create(pageItems, filtered.Length, request.Pagination.Page, request.Pagination.PageSize);
    }
}
