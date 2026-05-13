using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.Servers;

public sealed record ServerLocalizationDto
{
    public string Language { get; init; }
    public int CollationID { get; init; }
    public string ComparisonStyle { get; init; }
    public string SqlCharSetName { get; init; }
    public string SqlSortOrderName { get; init; }
}

public sealed record GetServerLocalizationRequest(string ServerName) : IRequest<ServerLocalizationDto>;

public sealed class GetServerLocalizationHandler(ILogger<GetServerLocalizationHandler> logger, IServerPort serverPort)
    : IRequestHandler<GetServerLocalizationRequest, ServerLocalizationDto>
{
    private readonly ILogger<GetServerLocalizationHandler> _logger = logger;
    private readonly IServerPort _serverPort = serverPort;

    public async ValueTask<ServerLocalizationDto> Handle(GetServerLocalizationRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting server localization for {ServerName}", request.ServerName);
        Server server = await _serverPort.GetServer(request.ServerName, cancellationToken);
        return new ServerLocalizationDto
        {
            Language = server.Language,
            CollationID = server.CollationID,
            ComparisonStyle = server.ComparisonStyle.ToString(),
            SqlCharSetName = server.SqlCharSetName,
            SqlSortOrderName = server.SqlSortOrderName
        };
    }
}
