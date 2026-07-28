# Server.Mcp.Shared — AGENTS.md

The wire-facing MCP tool layer. Every tool the product exposes lives here, and **both** hosts
run these same classes: `Server.Mcp` (stdio) and `Server.Api` (HTTP). Depends on
`Application` (Mediator) only — no SMO, no SQL.

How to author a tool (annotations, snake_case parameters, `CallToolResult`, never throw,
namespace rules, pagination) is **not** repeated here — see
[`mcp.md`](../../.claude/rules/mcp.md).

## Tools

Nine tools across six `[McpServerToolType]` classes in `tools/`:

| Class | Tool names |
| --- | --- |
| `ServerTools` | `get_server_info` |
| `DatabaseTools` | `list_databases`, `list_objects` |
| `TableTools` | `describe_table` |
| `ViewTools` | `describe_view` |
| `ProcedureTools` | `describe_procedure` |
| `SecurityTools` | `list_principals`, `list_role_memberships`, `list_permissions` |

Adding a tool means adding it to `AddTools()` in `DependencyInjection.cs` — registration is
explicit (`WithTools<T>()`), never assembly scanning.

## Boundaries

- **`tools/`** — the tool classes. Each is `internal sealed` with primary-constructor DI
  (`IMediator`, `IDefaultServerName`) and delegates straight to Application handlers.
- **`tools/Abstractions/`** — result shaping shared by ≥2 tools.
  - `ToolPayload` — the `CallToolResult` builder: `Structured<T>(payload)` for success,
    `MissingServerName()` for the one current error path.
  - `ServerInfoResult` — composite payload for `get_server_info`, fanning nine
    `GetServer*` Mediator requests into one record.
- **`Abstractions/`** (layer root) — layer-wide contracts, not tied to one tool.
  - `IDefaultServerName` — the per-host default data-source, supplied by DI.
  - `ServerNameResolver.TryResolve(requested, fallback, out serverName)` — prefers an
    explicit `server_name`, falls back to the host default, returns `false` when neither
    is available.
- **`DependencyInjection.cs`** — `AddTools()` (tool registration) and
  `AddDefaultServerName(string?)` (per-host default).

## Pattern (tool method)

1. `ServerNameResolver.TryResolve(server_name, _defaultServerName, out string resolved)`;
   return `ToolPayload.MissingServerName()` when it fails.
2. `await _mediator.Send(new SomeRequest(resolved, …), cancellationToken)`.
3. Return `ToolPayload.Structured(result)`.

Per-host behaviour comes only from DI: stdio registers
`AddDefaultServerName(MainConfigurationFactory.MainDataSourceName)` so `server_name` may be
omitted; HTTP registers `AddDefaultServerName(null)` so it is required. Never fork a tool
class per host.

See `tools/ServerTools.cs` as the reference implementation.
