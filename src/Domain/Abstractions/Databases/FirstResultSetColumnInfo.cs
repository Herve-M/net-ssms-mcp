namespace ssmsmcp.Domain.Abstractions.Databases;

public sealed record FirstResultSetColumnInfo(
    int Ordinal,
    string? Name,
    string? SystemTypeName,
    bool? IsNullable,
    int ErrorNumber);
