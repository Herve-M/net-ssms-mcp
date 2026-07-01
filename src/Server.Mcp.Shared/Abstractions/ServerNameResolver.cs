namespace ssmsmcp.Server.Mcp.Shared.Abstractions;

internal static class ServerNameResolver
{
    public static bool TryResolve(string? requested, IDefaultServerName fallback, out string serverName)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            serverName = requested;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fallback.Value))
        {
            serverName = fallback.Value;
            return true;
        }

        serverName = string.Empty;
        return false;
    }
}
