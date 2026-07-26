namespace ssmsmcp.Application.Abstractions;

public sealed record ForeignKeyRowDto
{
    public required string Name { get; init; }
    public required ObjectRefDto From { get; init; }
    public required ObjectRefDto To { get; init; }
    public required IReadOnlyList<string> FromColumns { get; init; }
    public required IReadOnlyList<string> ToColumns { get; init; }
    public required string DeleteAction { get; init; }
    public required string UpdateAction { get; init; }
    public required bool IsDisabled { get; init; }
    public required bool IsNotTrusted { get; init; }
}
