using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerAvailabilityDto
{
    public bool IsHadrEnabled { get; init; }
    public string HadrManagerStatus { get; init; }
    public bool IsClustered { get; init; }
    public string ClusterName { get; init; }
    public string ClusterQuorumType { get; init; }
    public string ClusterQuorumState { get; init; }
}

public sealed record GetServerAvailabilityRequest(string ServerName) : IRequest<ServerAvailabilityDto>;

public sealed class GetServerAvailabilityHandler(ILogger<GetServerAvailabilityHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerAvailabilityRequest, ServerAvailabilityDto>
{
    private readonly ILogger<GetServerAvailabilityHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerAvailabilityDto> Handle(GetServerAvailabilityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server availability for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerAvailabilityDto
        {
            IsHadrEnabled = server.IsHadrEnabled,
            HadrManagerStatus = server.HadrManagerStatus.ToString(),
            IsClustered = server.IsClustered,
            ClusterName = server.ClusterName,
            ClusterQuorumType = server.ClusterQuorumType.ToString(),
            ClusterQuorumState = server.ClusterQuorumState.ToString()
        };
    }
}
