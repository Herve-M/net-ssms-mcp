using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public sealed record DatabaseStorageDto
{
    public double Size { get; init; }
    public double SpaceAvailable { get; init; }
    public double MaxSizeInBytes { get; init; }
    public double DataSpaceUsage { get; init; }
    public double IndexSpaceUsage { get; init; }
    public DateTime LastBackupDate { get; init; }
    public DateTime LastDifferentialBackupDate { get; init; }
    public DateTime LastLogBackupDate { get; init; }
    public DateTime LastGoodCheckDbTime { get; init; }
    public string DefaultFileGroup { get; init; }
    public string PrimaryFilePath { get; init; }
    public string DefaultFullTextCatalog { get; init; }
}

public sealed record GetDatabaseStorageRequest(string ServerName, string DatabaseName) : IRequest<DatabaseStorageDto>;

public sealed class GetDatabaseStorageHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseStorageRequest, DatabaseStorageDto>
{
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DatabaseStorageDto> Handle(GetDatabaseStorageRequest request, CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);

        return new DatabaseStorageDto
        {
            Size = database.Size,
            SpaceAvailable = database.SpaceAvailable,
            // MaxSizeInBytes = database.MaxSizeInBytes,
            DataSpaceUsage = database.DataSpaceUsage,
            IndexSpaceUsage = database.IndexSpaceUsage,
            LastBackupDate = database.LastBackupDate,
            LastDifferentialBackupDate = database.LastDifferentialBackupDate,
            LastLogBackupDate = database.LastLogBackupDate,
            LastGoodCheckDbTime = database.LastGoodCheckDbTime,
            DefaultFileGroup = database.DefaultFileGroup,
            PrimaryFilePath = database.PrimaryFilePath,
            DefaultFullTextCatalog = database.DefaultFullTextCatalog
        };
    }
}