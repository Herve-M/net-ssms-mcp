namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerLocalizationResponse
{
    public string Language { get; init; }
    public int CollationID { get; init; }
    public string ComparisonStyle { get; init; }
    public string SqlCharSetName { get; init; }
    public string SqlSortOrderName { get; init; }
}
