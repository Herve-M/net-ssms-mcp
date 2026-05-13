namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerConnectivityResponse
{
    public bool TcpEnabled { get; init; }
    public bool NamedPipesEnabled { get; init; }
    public string NetName { get; init; }
}
