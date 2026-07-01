namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseUserDefinedTypeResponse
{
    public required string Name { get; init; }
}