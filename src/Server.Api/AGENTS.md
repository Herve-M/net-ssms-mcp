# Server.Api — AGENTS.md

The HTTP host, serving **two** surfaces from one process over the same Application handlers:

1. a versioned REST API (`Controllers/` + `Models/V1/`), and
2. the HTTP MCP endpoint (`MapMcp("/mcp")`, stateless), running the shared tool classes from
   `Server.Mcp.Shared`.

Conventions: [`rest-api.md`](../../.claude/rules/rest-api.md) for the REST surface,
[`mcp.md`](../../.claude/rules/mcp.md) for the MCP surface. Neither is repeated here.

## Multi-server, unlike the stdio host

This host registers `AddDefaultServerName(null)` and loads its data-sources from
`configs/main.json` via `WithFileConfiguration(...)`. Consequence: `server_name` is
**required** on every MCP tool call — omitting it returns `ToolPayload.MissingServerName()`.
The stdio host is the opposite (defaults to `"main"`). This asymmetry is intentional; it comes
from DI registration alone, never from forking a tool class.

## Boundaries

- **`Program.cs`** — composition root. Controllers + API versioning (`AddApiVersioning`,
  `AddApiExplorer`, `AddOpenApi`), the MCP server (`AddMcpServer(...).AddTools()`),
  `AddDefaultServerName(null)`, the Infrastructure fluent chain, `UseApplicationLayer`,
  Mediator. Endpoints: `MapMcp("/mcp")`, `MapControllers()`, `MapHealthChecks("/health")`,
  plus `MapOpenApi()` / `MapScalarApiReference("/docs")` and the `McpInspector` CORS policy in
  Development only.
- **`Controllers/`** — `ServersController`, `DatabasesController`. Thin: they send a Mediator
  request and map the result to a `Models/V1` response.
- **`Models/V1/`** — versioned `*Response` records, one per query
  (`ServerEngineResponse`, `DatabaseTableResponse`, …). These are the REST contract and are
  **separate** from the MCP tool payloads — the two surfaces share handlers, not DTOs.
- **`configs/main.json`** — the data-source list for this host.

## Adding a query

The Application handler is written once; then expose it on whichever surface needs it — a
controller action plus a `Models/V1` response for REST, a tool method in `Server.Mcp.Shared`
for MCP. Do not add business logic in either place.
