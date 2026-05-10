namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerDatabaseListItemResponse
{
    public Guid Id { get; init; }
    public string DatabaseName { get; init; }
}
