namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseAvailabilityResponse
{
    public string AvailabilityGroupName { get; init; }
    public string AvailabilityDatabaseSynchronizationState { get; init; }
    public bool IsMirroringEnabled { get; init; }
    public string MirroringStatus { get; init; }
    public string MirroringPartner { get; init; }
    public Guid ServiceBrokerGuid { get; init; }
    public bool BrokerEnabled { get; init; }
    public string ReplicationOptions { get; init; }
    public string LogReuseWaitStatus { get; init; }
}