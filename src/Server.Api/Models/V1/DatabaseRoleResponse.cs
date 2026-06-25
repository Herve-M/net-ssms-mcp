namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseRoleResponse
{
    public required string Name { get; init; }
}