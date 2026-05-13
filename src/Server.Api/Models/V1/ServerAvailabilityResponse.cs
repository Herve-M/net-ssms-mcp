namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerAvailabilityResponse
{
    public bool IsHadrEnabled { get; init; }
    public string HadrManagerStatus { get; init; }
    public bool IsClustered { get; init; }
    public string ClusterName { get; init; }
    public string ClusterQuorumType { get; init; }
    public string ClusterQuorumState { get; init; }
}
