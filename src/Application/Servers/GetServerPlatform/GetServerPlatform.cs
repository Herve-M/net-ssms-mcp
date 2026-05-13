using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerPlatformDto
{
    public string HostPlatform { get; init; }
    public string HostDistribution { get; init; }
    public string HostRelease { get; init; }
    public string HostServicePackLevel { get; init; }
    public string Platform { get; init; }
    public string OSVersion { get; init; }
}

public sealed record GetServerPlatformRequest(string ServerName) : IRequest<ServerPlatformDto>;

public sealed class GetServerPlatformHandler(ILogger<GetServerPlatformHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerPlatformRequest, ServerPlatformDto>
{
    private readonly ILogger<GetServerPlatformHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerPlatformDto> Handle(GetServerPlatformRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server platform for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerPlatformDto
        {
            HostPlatform = server.HostPlatform,
            HostDistribution = server.HostDistribution,
            HostRelease = server.HostRelease,
            HostServicePackLevel = server.HostServicePackLevel,
            Platform = server.Platform,
            OSVersion = server.OSVersion
        };
    }
}
