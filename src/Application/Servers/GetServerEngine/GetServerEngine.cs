using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerEngineDto
{
    public string Edition { get; init; }
    public string EngineEdition { get; init; }
    public string ServerType { get; init; }
    public bool IsSingleUser { get; init; }
    public bool IsCaseSensitive { get; init; }
    public bool IsXTPSupported { get; init; }
}

public sealed record GetServerEngineRequest(string ServerName) : IRequest<ServerEngineDto>;

public sealed class GetServerEngineHandler(ILogger<GetServerEngineHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerEngineRequest, ServerEngineDto>
{
    private readonly ILogger<GetServerEngineHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerEngineDto> Handle(GetServerEngineRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server engine for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerEngineDto
        {
            Edition = server.Edition,
            EngineEdition = server.EngineEdition.ToString(),
            ServerType = server.ServerType.ToString(),
            IsSingleUser = server.IsSingleUser,
            IsCaseSensitive = server.IsCaseSensitive,
            IsXTPSupported = server.IsXTPSupported
        };
    }
}
