namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseTableResponse
{
    public required string Name { get; init; }
}