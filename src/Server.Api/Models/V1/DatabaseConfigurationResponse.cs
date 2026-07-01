namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseConfigurationResponse
{
    public string Collation { get; init; }
    public string CompatibilityLevel { get; init; }
    public string ContainmentType { get; init; }
    public string RecoveryModel { get; init; }
    public bool AnsiNullDefault { get; init; }
    public bool AnsiNullsEnabled { get; init; }
    public bool AnsiPaddingEnabled { get; init; }
    public bool AnsiWarningsEnabled { get; init; }
    public bool ArithmeticAbortEnabled { get; init; }
    public bool AutoClose { get; init; }
    public bool AutoShrink { get; init; }
    public bool AutoUpdateStatisticsEnabled { get; init; }
    public bool AutoUpdateStatisticsAsync { get; init; }
    public bool AutoCreateStatisticsEnabled { get; init; }
    public bool AutoCreateIncrementalStatisticsEnabled { get; init; }
    public bool IsParameterizationForced { get; init; }
    public bool RecursiveTriggersEnabled { get; init; }
    public bool NestedTriggersEnabled { get; init; }
    public string PageVerify { get; init; }
    public bool QuotedIdentifiersEnabled { get; init; }
    public bool NumericRoundAbortEnabled { get; init; }
}