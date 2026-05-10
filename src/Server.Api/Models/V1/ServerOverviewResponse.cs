namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerOverviewResponse
{
    public string ServerName { get; init; }
    public string InstanceName { get; init; }
    public string Edition { get; init; }
    public string EngineEdition { get; init; }
    public string VersionString { get; init; }
    public string ProductLevel { get; init; }
    public string ProductUpdateLevel { get; init; }
    public string Status { get; init; }
}
