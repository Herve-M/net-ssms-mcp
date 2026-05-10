using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerCapacityDto
{
    public int Processors { get; init; }
    public int PhysicalMemory { get; init; }
    public long PhysicalMemoryUsageInKB { get; init; }
    public int ReservedStorageSizeMB { get; init; }
    public int UsedStorageSizeMB { get; init; }
    public int NumberOfLogFiles { get; init; }
}

public sealed record GetServerCapacityRequest(string ServerName) : IRequest<ServerCapacityDto>;

public sealed class GetServerCapacityHandler(ILogger<GetServerCapacityHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerCapacityRequest, ServerCapacityDto>
{
    private readonly ILogger<GetServerCapacityHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerCapacityDto> Handle(GetServerCapacityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server capacity for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerCapacityDto
        {
            Processors = server.Processors,
            PhysicalMemory = server.PhysicalMemory,
            PhysicalMemoryUsageInKB = server.PhysicalMemoryUsageInKB,
            ReservedStorageSizeMB = server.ReservedStorageSizeMB,
            UsedStorageSizeMB = server.UsedStorageSizeMB,
            NumberOfLogFiles = server.NumberOfLogFiles
        };
    }
}
