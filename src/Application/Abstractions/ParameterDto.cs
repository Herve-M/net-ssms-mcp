namespace ssmsmcp.Application.Abstractions;

public sealed record ParameterDto
{
    public required string Name { get; init; }
    public required int Ordinal { get; init; }
    public required string DataType { get; init; }
    public required bool IsOutput { get; init; }
    public required bool IsReadonly { get; init; }
    public required bool HasDefaultValue { get; init; }
    public string? DefaultValue { get; init; }
}
