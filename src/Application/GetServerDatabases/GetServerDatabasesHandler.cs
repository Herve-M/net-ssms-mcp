using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Servers;

namespace ssmsmcp.Application.GetServerDatabases;

public record GetServerDatabasesRequest(string ServerName) : IRequest<IReadOnlyCollection<DatabaseSummary>>;

public sealed record DatabaseSummary
{
    public string? DatabaseName { get; init; }
}

public sealed class GetServerDatabasesHandler
    : IRequestHandler<GetServerDatabasesRequest, IReadOnlyCollection<DatabaseSummary>>
{
    private readonly ILogger<GetServerDatabasesHandler> _logger;
    private readonly IServerPort _serverPort;

    public GetServerDatabasesHandler(ILogger<GetServerDatabasesHandler> logger, IServerPort serverPort)
    {
        _serverPort = serverPort;
        _logger = logger;
    }

    public async ValueTask<IReadOnlyCollection<DatabaseSummary>> Handle(
        GetServerDatabasesRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Database> databases = await _serverPort.GetDatabases(request.ServerName, cancellationToken);

        return databases
            .Select(database => new DatabaseSummary
            {
                DatabaseName = database.Name
            })
            .ToArray();
    }
}
