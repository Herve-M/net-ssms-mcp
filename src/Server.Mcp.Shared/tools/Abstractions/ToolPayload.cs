using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace ssmsmcp.Server.Mcp.tools.Abstractions;

internal static class ToolPayload
{
    public static CallToolResult Structured<T>(T payload)
    {
        JsonElement structured = JsonSerializer.SerializeToElement(payload, McpJsonUtilities.DefaultOptions);
        string text = JsonSerializer.Serialize(payload, McpJsonUtilities.DefaultOptions);

        return new CallToolResult
        {
            StructuredContent = structured,
            Content = [new TextContentBlock { Text = text }],
        };
    }

    public static CallToolResult MissingServerName()
    {
        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = "A server_name argument is required; no default data-source is configured on this host.",
                },
            ],
        };
    }
}
