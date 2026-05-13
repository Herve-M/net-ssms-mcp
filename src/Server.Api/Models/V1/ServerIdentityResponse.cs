namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerIdentityResponse
{
    public string ServerName { get; init; }
    public string InstanceName { get; init; }
    public string NetName { get; init; }
    public string ComputerNamePhysicalNetBIOS { get; init; }
    public string ServiceName { get; init; }
    public string ServiceInstanceId { get; init; }
}
