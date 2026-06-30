namespace ssmsmcp.Domain.Abstractions.Databases;

public sealed record DatabaseObjectInfo(string Schema, string Name, string Type, string Urn);
