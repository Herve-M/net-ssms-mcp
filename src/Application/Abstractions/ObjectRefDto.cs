namespace ssmsmcp.Application.Abstractions;

public sealed record ObjectRefDto
{
    public required string Database { get; init; }
    public required string Schema { get; init; }
    public required string Name { get; init; }
    public required int ObjectId { get; init; }
    public required string TypeDesc { get; init; }
    public required string Fqn { get; init; }
}
