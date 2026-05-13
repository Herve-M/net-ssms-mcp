namespace ssmsmcp.Server.Api.Models.V1;

public sealed record ServerStorageResponse
{
    public string BackupDirectory { get; init; }
    public string DefaultFile { get; init; }
    public string DefaultLog { get; init; }
    public string MasterDBPath { get; init; }
    public string MasterDBLogPath { get; init; }
    public string ErrorLogPath { get; init; }
}
