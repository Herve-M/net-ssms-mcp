using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerFeaturesDto
{
    public bool IsJsonDataTypeEnabled { get; init; }
    public bool IsXTPSupported { get; init; }
    public bool IsPolyBaseInstalled { get; init; }
    public bool IsFullTextInstalled { get; init; }
    public string FilestreamLevel { get; init; }
    public string FilestreamShareName { get; init; }
}

public sealed record GetServerFeaturesRequest(string ServerName) : IRequest<ServerFeaturesDto>;

public sealed class GetServerFeaturesHandler(ILogger<GetServerFeaturesHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerFeaturesRequest, ServerFeaturesDto>
{
    private readonly ILogger<GetServerFeaturesHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerFeaturesDto> Handle(GetServerFeaturesRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server features for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerFeaturesDto
        {
            IsJsonDataTypeEnabled = server.IsJsonDataTypeEnabled,
            IsXTPSupported = server.IsXTPSupported,
            IsPolyBaseInstalled = server.IsPolyBaseInstalled,
            IsFullTextInstalled = server.IsFullTextInstalled,
            FilestreamLevel = server.FilestreamLevel.ToString(),
            FilestreamShareName = server.FilestreamShareName
        };
    }
}
