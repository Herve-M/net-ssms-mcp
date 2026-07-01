namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseUserResponse
{
    public required string Name { get; init; }
}