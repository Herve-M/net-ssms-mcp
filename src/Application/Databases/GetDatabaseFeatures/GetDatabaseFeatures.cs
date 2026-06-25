using Mediator;
using Microsoft.SqlServer.Management.Smo;
using ssmsmcp.Domain.Abstractions.Databases;

namespace ssmsmcp.Application.Databases;

public abstract record DatabaseFeaturesDto
{
    public abstract string EngineType { get; }
    public bool IsFullTextEnabled { get; init; }
    public bool ChangeTrackingEnabled { get; init; }
    public bool ChangeTrackingAutoCleanUp { get; init; }
    public int ChangeTrackingRetentionPeriod { get; init; }
    public string ChangeTrackingRetentionPeriodUnits { get; init; }
    public bool IsLedger { get; init; }
    public bool HasMemoryOptimizedObjects { get; init; }
    public double MemoryAllocatedToMemoryOptimizedObjectsInKB { get; init; }
    public double MemoryUsedByMemoryOptimizedObjectsInKB { get; init; }
}

public sealed record StandaloneDatabaseFeaturesDto : DatabaseFeaturesDto
{
    public override string EngineType => "Standalone";
    public string FilestreamDirectoryName { get; init; }
    public string FilestreamNonTransactedAccess { get; init; }
}

public sealed record SqlAzureDatabaseFeaturesDto : DatabaseFeaturesDto
{
    public override string EngineType => "SqlAzureDatabase";
    public bool TemporalHistoryRetentionEnabled { get; init; }
    public bool IsSqlDw { get; init; }
    public bool IsSqlDwEdition { get; init; }
}

public sealed record GetDatabaseFeaturesRequest(string ServerName, string DatabaseName) : IRequest<DatabaseFeaturesDto>;

public sealed class GetDatabaseFeaturesHandler(IDatabasePort databasePort)
    : IRequestHandler<GetDatabaseFeaturesRequest, DatabaseFeaturesDto>
{
    private readonly IDatabasePort _databasePort = databasePort;

    public async ValueTask<DatabaseFeaturesDto> Handle(GetDatabaseFeaturesRequest request, CancellationToken cancellationToken)
    {
        Database database = await _databasePort.GetDatabase(request.ServerName, request.DatabaseName, cancellationToken);

        if (database.DatabaseEngineType == Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
        {
            return new SqlAzureDatabaseFeaturesDto
            {
                IsFullTextEnabled = database.IsFullTextEnabled,
                ChangeTrackingEnabled = database.ChangeTrackingEnabled,
                ChangeTrackingAutoCleanUp = database.ChangeTrackingAutoCleanUp,
                ChangeTrackingRetentionPeriod = database.ChangeTrackingRetentionPeriod,
                ChangeTrackingRetentionPeriodUnits = database.ChangeTrackingRetentionPeriodUnits.ToString(),
                IsLedger = database.IsLedger,
                TemporalHistoryRetentionEnabled = database.TemporalHistoryRetentionEnabled,
                HasMemoryOptimizedObjects = database.HasMemoryOptimizedObjects,
                MemoryAllocatedToMemoryOptimizedObjectsInKB = database.MemoryAllocatedToMemoryOptimizedObjectsInKB,
                MemoryUsedByMemoryOptimizedObjectsInKB = database.MemoryUsedByMemoryOptimizedObjectsInKB,
                IsSqlDw = database.IsSqlDw,
                IsSqlDwEdition = database.IsSqlDwEdition
            };
        }

        return new StandaloneDatabaseFeaturesDto
        {
            IsFullTextEnabled = database.IsFullTextEnabled,
            ChangeTrackingEnabled = database.ChangeTrackingEnabled,
            ChangeTrackingAutoCleanUp = database.ChangeTrackingAutoCleanUp,
            ChangeTrackingRetentionPeriod = database.ChangeTrackingRetentionPeriod,
            ChangeTrackingRetentionPeriodUnits = database.ChangeTrackingRetentionPeriodUnits.ToString(),
            IsLedger = database.IsLedger,
            HasMemoryOptimizedObjects = database.HasMemoryOptimizedObjects,
            MemoryAllocatedToMemoryOptimizedObjectsInKB = database.MemoryAllocatedToMemoryOptimizedObjectsInKB,
            MemoryUsedByMemoryOptimizedObjectsInKB = database.MemoryUsedByMemoryOptimizedObjectsInKB,
            FilestreamDirectoryName = database.FilestreamDirectoryName,
            FilestreamNonTransactedAccess = database.FilestreamNonTransactedAccess.ToString()
        };
    }
}