using System.Text.Json.Serialization;

namespace ssmsmcp.Server.Api.Models.V1;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "engineType")]
[JsonDerivedType(typeof(StandaloneDatabaseFeaturesResponse), typeDiscriminator: "Standalone")]
[JsonDerivedType(typeof(SqlAzureDatabaseFeaturesResponse), typeDiscriminator: "SqlAzureDatabase")]
public abstract record DatabaseFeaturesResponse
{
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

public sealed record StandaloneDatabaseFeaturesResponse : DatabaseFeaturesResponse
{
    public string FilestreamDirectoryName { get; init; }
    public string FilestreamNonTransactedAccess { get; init; }
}

public sealed record SqlAzureDatabaseFeaturesResponse : DatabaseFeaturesResponse
{
    public bool TemporalHistoryRetentionEnabled { get; init; }
    public bool IsSqlDw { get; init; }
    public bool IsSqlDwEdition { get; init; }
}