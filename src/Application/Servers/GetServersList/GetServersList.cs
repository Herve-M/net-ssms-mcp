using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerListItemDto
{
    public string ServerName { get; init; }
    public string ServerVersion { get; init; }
    public string DatabaseEngineType { get; init; }
    public string DatabaseEngineEdition { get; init; }
}

public sealed record GetServersListRequest : IRequest<IReadOnlyCollection<ServerListItemDto>>;

public sealed class GetServersListHandler(ILogger<GetServersListHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServersListRequest, IReadOnlyCollection<ServerListItemDto>>
{
    private readonly ILogger<GetServersListHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<IReadOnlyCollection<ServerListItemDto>> Handle(GetServersListRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server list");
        IReadOnlyCollection<Server> servers = await _serverPort.GetServers(cancellationToken);

        return servers
            .Select(server => new ServerListItemDto
            {
                ServerName = server.Name,
                ServerVersion = server.VersionString,
                DatabaseEngineType = server.DatabaseEngineType.ToString(),
                DatabaseEngineEdition = server.DatabaseEngineEdition.ToString()
            })
            .ToArray();
    }
}
