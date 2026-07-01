namespace ssmsmcp.Server.Api.Models.V1;

public sealed record DatabaseDetailsResponse
{
	public string DatabaseName { get; init; }
	public int Id { get; init; }
	public Guid DatabaseGuid { get; init; }
	public string Status { get; init; }
	public bool IsAccessible { get; init; }
	public bool IsSystemObject { get; init; }
	public bool IsDatabaseSnapshot { get; init; }
	public bool IsFabricDatabase { get; init; }
	public DateTime CreateDate { get; init; }
}
