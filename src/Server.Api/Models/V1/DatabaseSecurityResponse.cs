namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseSecurityResponse
{
    public string Owner { get; init; }
    public string UserAccess { get; init; }
    public bool Trustworthy { get; init; }
    public bool DatabaseOwnershipChaining { get; init; }
    public bool EncryptionEnabled { get; init; }
    public bool HasDatabaseEncryptionKey { get; init; }
    public bool DboLogin { get; init; }
    public bool IsDbOwner { get; init; }
    public bool IsDbSecurityAdmin { get; init; }
    public bool IsDbAccessAdmin { get; init; }
    public bool IsDbDatareader { get; init; }
    public bool IsDbDatawriter { get; init; }
    public bool IsDbDdlAdmin { get; init; }
}