namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseStorageResponse
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