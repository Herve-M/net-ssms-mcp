namespace ssmsmcp.Server.Mcp.Shared.Abstractions;

public interface IDefaultServerName
{
    string? Value { get; }
}

internal sealed class DefaultServerName(string? value) : IDefaultServerName
{
    public string? Value { get; } = value;
}
