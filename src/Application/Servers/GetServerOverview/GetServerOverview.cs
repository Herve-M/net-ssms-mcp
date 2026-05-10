using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerOverviewDto
{
    public string ServerName { get; init; }
    public string InstanceName { get; init; }
    public string Edition { get; init; }
    public string EngineEdition { get; init; }
    public string VersionString { get; init; }
    public string ProductLevel { get; init; }
    public string ProductUpdateLevel { get; init; }
    public string Status { get; init; }
}

public sealed record GetServerOverviewRequest(string ServerName) : IRequest<ServerOverviewDto>;

public sealed class GetServerOverviewHandler(ILogger<GetServerOverviewHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerOverviewRequest, ServerOverviewDto>
{
    private readonly ILogger<GetServerOverviewHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerOverviewDto> Handle(GetServerOverviewRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server overview for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerOverviewDto
        {
            ServerName = server.Name,
            InstanceName = server.InstanceName,
            Edition = server.Edition,
            EngineEdition = server.EngineEdition.ToString(),
            VersionString = server.VersionString,
            ProductLevel = server.ProductLevel,
            ProductUpdateLevel = server.ProductUpdateLevel,
            Status = server.Status.ToString()
        };
    }
}
