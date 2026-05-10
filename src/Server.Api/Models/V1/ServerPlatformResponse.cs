namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerPlatformResponse
{
    public string HostPlatform { get; init; }
    public string HostDistribution { get; init; }
    public string HostRelease { get; init; }
    public string HostServicePackLevel { get; init; }
    public string Platform { get; init; }
    public string OSVersion { get; init; }
}
