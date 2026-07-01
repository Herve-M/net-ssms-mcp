namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseStoredProcedureResponse
{
    public required string Name { get; init; }
}