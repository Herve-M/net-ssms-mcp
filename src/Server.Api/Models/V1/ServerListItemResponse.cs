namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerListItemResponse
{
    public string ServerName { get; init; }

    public string ServerVersion { get; init; }

    public string DatabaseEngineType { get; init; }

    public string DatabaseEngineEdition { get; init; }
}
