namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseViewResponse
{
    public required string Name { get; init; }
}