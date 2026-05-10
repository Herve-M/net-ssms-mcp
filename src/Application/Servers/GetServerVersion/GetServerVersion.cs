using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerVersionDto
{
    public int BuildNumber { get; init; }
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public string VersionString { get; init; }
    public string Product { get; init; }
    public string ProductLevel { get; init; }
    public string ProductUpdateLevel { get; init; }
    public string ResourceVersionString { get; init; }
}

public sealed record GetServerVersionRequest(string ServerName) : IRequest<ServerVersionDto>;

public sealed class GetServerVersionHandler(ILogger<GetServerVersionHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerVersionRequest, ServerVersionDto>
{
    private readonly ILogger<GetServerVersionHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerVersionDto> Handle(GetServerVersionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server version for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerVersionDto
        {
            BuildNumber = server.BuildNumber,
            VersionMajor = server.VersionMajor,
            VersionMinor = server.VersionMinor,
            VersionString = server.VersionString,
            Product = server.Product,
            ProductLevel = server.ProductLevel,
            ProductUpdateLevel = server.ProductUpdateLevel,
            ResourceVersionString = server.ResourceVersionString
        };
    }
}
