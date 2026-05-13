using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerConnectivityDto
{
    public bool TcpEnabled { get; init; }
    public bool NamedPipesEnabled { get; init; }
    public string NetName { get; init; }
}

public sealed record GetServerConnectivityRequest(string ServerName) : IRequest<ServerConnectivityDto>;

public sealed class GetServerConnectivityHandler(ILogger<GetServerConnectivityHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerConnectivityRequest, ServerConnectivityDto>
{
    private readonly ILogger<GetServerConnectivityHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerConnectivityDto> Handle(GetServerConnectivityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server connectivity for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerConnectivityDto
        {
            TcpEnabled = server.TcpEnabled,
            NamedPipesEnabled = server.NamedPipesEnabled,
            NetName = server.NetName
        };
    }
}
