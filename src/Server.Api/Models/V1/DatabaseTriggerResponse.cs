namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseTriggerResponse
{
    public required string Name { get; init; }
}