using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerStorageDto
{
    public string BackupDirectory { get; init; }
    public string DefaultFile { get; init; }
    public string DefaultLog { get; init; }
    public string MasterDBPath { get; init; }
    public string MasterDBLogPath { get; init; }
    public string ErrorLogPath { get; init; }
}

public sealed record GetServerStorageRequest(string ServerName) : IRequest<ServerStorageDto>;

public sealed class GetServerStorageHandler(ILogger<GetServerStorageHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerStorageRequest, ServerStorageDto>
{
    private readonly ILogger<GetServerStorageHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerStorageDto> Handle(GetServerStorageRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server storage for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerStorageDto
        {
            BackupDirectory = server.BackupDirectory,
            DefaultFile = server.DefaultFile,
            DefaultLog = server.DefaultLog,
            MasterDBPath = server.MasterDBPath,
            MasterDBLogPath = server.MasterDBLogPath,
            ErrorLogPath = server.ErrorLogPath
        };
    }
}
