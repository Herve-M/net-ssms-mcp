namespace ssmsmcp.Server.Api.Models.V1;

/// <summary>
/// Response model representing a database schema.
/// </summary>
public sealed record DatabaseSchemaResponse
{
    /// <summary>
    /// Gets or sets the schema ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    public required string Name { get; init; }
}
