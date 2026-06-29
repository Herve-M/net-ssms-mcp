using ssmsmcp.Application.Servers;

namespace ssmsmcp.Server.Mcp.tools.Abstractions;

public sealed record ServerInfoResult
{
    public required ServerOverviewDto Overview { get; init; }
    public required ServerVersionDto Version { get; init; }
    public required ServerPlatformDto Platform { get; init; }
    public required ServerFeaturesDto Features { get; init; }
    public required ServerSecurityDto Security { get; init; }
    public required ServerEngineDto Engine { get; init; }
    public required ServerIdentityDto Identity { get; init; }
    public required ServerLocalizationDto Localization { get; init; }
    public required ServerAvailabilityDto Availability { get; init; }
}
