using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerIdentityDto
{
    public string ServerName { get; init; }
    public string InstanceName { get; init; }
    public string NetName { get; init; }
    public string ComputerNamePhysicalNetBIOS { get; init; }
    public string ServiceName { get; init; }
    public string ServiceInstanceId { get; init; }
}

public sealed record GetServerIdentityRequest(string ServerName) : IRequest<ServerIdentityDto>;

public sealed class GetServerIdentityHandler(ILogger<GetServerIdentityHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerIdentityRequest, ServerIdentityDto>
{
    private readonly ILogger<GetServerIdentityHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerIdentityDto> Handle(GetServerIdentityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server identity for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerIdentityDto
        {
            ServerName = server.Name,
            InstanceName = server.InstanceName,
            NetName = server.NetName,
            ComputerNamePhysicalNetBIOS = server.ComputerNamePhysicalNetBIOS,
            ServiceName = server.ServiceName,
            ServiceInstanceId = server.ServiceInstanceId
        };
    }
}
