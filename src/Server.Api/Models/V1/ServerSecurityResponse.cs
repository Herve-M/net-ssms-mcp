namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerSecurityResponse
{
    public string LoginMode { get; init; }
    public string AuditLevel { get; init; }
    public string ServiceAccount { get; init; }
    public bool IsContainedAuthentication { get; init; }
    public string SqlDomainGroup { get; init; }
}
