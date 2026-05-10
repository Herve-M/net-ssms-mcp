namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerEngineResponse
{
    public string Edition { get; init; }
    public string EngineEdition { get; init; }
    public string ServerType { get; init; }
    public bool IsSingleUser { get; init; }
    public bool IsCaseSensitive { get; init; }
    public bool IsXTPSupported { get; init; }
}
