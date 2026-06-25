using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseDetailsDto
{
    public string DatabaseName { get; init; }
    public int Id { get; init; }
    public Guid DatabaseGuid { get; init; }
    public string Status { get; init; }
    public bool IsAccessible { get; init; }
    public bool IsSystemObject { get; init; }
    public bool IsDatabaseSnapshot { get; init; }
    public bool IsFabricDatabase { get; init; }
    public DateTime CreateDate { get; init; }
}

public sealed record GetDatabaseDetailsRequest(string ServerName, string DatabaseName) : IRequest<DatabaseDetailsDto>;

public sealed class GetDatabaseDetailsHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseDetailsRequest, DatabaseDetailsDto>
{
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DatabaseDetailsDto> Handle(GetDatabaseDetailsRequest request, CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);

        return new DatabaseDetailsDto
        {
            DatabaseName = database.Name,
            Id = database.ID,
            DatabaseGuid = database.DatabaseGuid,
            Status = database.Status.ToString(),
            IsAccessible = database.IsAccessible,
            IsSystemObject = database.IsSystemObject,
            IsDatabaseSnapshot = database.IsDatabaseSnapshot,
            IsFabricDatabase = database.IsFabricDatabase,
            CreateDate = database.CreateDate
        };
    }
}