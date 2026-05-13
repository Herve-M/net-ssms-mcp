using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerSecurityDto
{
    public string LoginMode { get; init; }
    public string AuditLevel { get; init; }
    public string ServiceAccount { get; init; }
    public bool IsContainedAuthentication { get; init; }
    public string SqlDomainGroup { get; init; }
}

public sealed record GetServerSecurityRequest(string ServerName) : IRequest<ServerSecurityDto>;

public sealed class GetServerSecurityHandler(ILogger<GetServerSecurityHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerSecurityRequest, ServerSecurityDto>
{
    private readonly ILogger<GetServerSecurityHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerSecurityDto> Handle(GetServerSecurityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server security for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerSecurityDto
        {
            LoginMode = server.LoginMode.ToString(),
            AuditLevel = server.AuditLevel.ToString(),
            ServiceAccount = server.ServiceAccount,
            IsContainedAuthentication = false,
            SqlDomainGroup = server.SqlDomainGroup
        };
    }
}
