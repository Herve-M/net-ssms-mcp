namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerCapacityResponse
{
    public int Processors { get; init; }
    public int PhysicalMemory { get; init; }
    public long PhysicalMemoryUsageInKB { get; init; }
    public int ReservedStorageSizeMB { get; init; }
    public int UsedStorageSizeMB { get; init; }
    public int NumberOfLogFiles { get; init; }
}
