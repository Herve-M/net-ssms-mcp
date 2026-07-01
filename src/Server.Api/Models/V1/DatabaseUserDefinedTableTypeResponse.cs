namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseUserDefinedTableTypeResponse
{
    public required string Name { get; init; }
}