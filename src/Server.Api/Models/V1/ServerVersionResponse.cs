namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerVersionResponse
{
    public int BuildNumber { get; init; }
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public string VersionString { get; init; }
    public string Product { get; init; }
    public string ProductLevel { get; init; }
    public string ProductUpdateLevel { get; init; }
    public string ResourceVersionString { get; init; }
}
