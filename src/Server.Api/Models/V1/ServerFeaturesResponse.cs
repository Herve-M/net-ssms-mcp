namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerFeaturesResponse
{
    public bool IsJsonDataTypeEnabled { get; init; }
    public bool IsXTPSupported { get; init; }
    public bool IsPolyBaseInstalled { get; init; }
    public bool IsFullTextInstalled { get; init; }
    public string FilestreamLevel { get; init; }
    public string FilestreamShareName { get; init; }
}
