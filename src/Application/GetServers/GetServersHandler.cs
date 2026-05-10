using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.GetServers;

public record GetServersRequest : IRequest<IReadOnlyCollection<ServerSummary>>;

public sealed record ServerSummary
{
    public string? ServerName { get; init; }

    public string? InstanceVersion { get; init; }
}

public sealed class GetServersHandler
    : IRequestHandler<GetServersRequest, IReadOnlyCollection<ServerSummary>>
{
    private readonly ILogger<GetServersHandler> _logger;
    private readonly IServerPort _serverPort;

    public GetServersHandler(ILogger<GetServersHandler> logger, IServerPort serverPort)
    {
        _serverPort = serverPort;
        _logger = logger;
    }

    public async ValueTask<IReadOnlyCollection<ServerSummary>> Handle(
        GetServersRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Server> servers = await _serverPort.GetServers(cancellationToken);

        return servers
            .Select(server => new ServerSummary
            {
                ServerName = server.Name,
                InstanceVersion = server.VersionString
            })
            .ToArray();
    }
}
