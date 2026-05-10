using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerDatabaseListItemDto
{
    public Guid Id { get; init; }
    public string DatabaseName { get; init; }
}

public sealed record GetServerDatabasesRequest(string ServerName) : IRequest<IReadOnlyCollection<ServerDatabaseListItemDto>>;

public sealed class GetServerDatabasesHandler(ILogger<GetServerDatabasesHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerDatabasesRequest, IReadOnlyCollection<ServerDatabaseListItemDto>>
{
    private readonly ILogger<GetServerDatabasesHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<IReadOnlyCollection<ServerDatabaseListItemDto>> Handle(GetServerDatabasesRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting databases for server {ServerName}", request.ServerName);
        IReadOnlyCollection<Database> databases = await _serverPort.GetDatabases(request.ServerName, cancellationToken);

        return databases
            .Select(database => new ServerDatabaseListItemDto
            {
                Id = database.DatabaseGuid,
                DatabaseName = database.Name
            })
            .ToArray();
    }
}
