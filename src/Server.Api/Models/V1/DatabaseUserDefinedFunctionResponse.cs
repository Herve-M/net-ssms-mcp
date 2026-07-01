namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseUserDefinedFunctionResponse
{
    public required string Name { get; init; }
}