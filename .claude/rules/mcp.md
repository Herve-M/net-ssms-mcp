---
description: "MCP guidelines"
paths: ["**/src/Server.Mcp.Shared/**/*.cs", "**/src/Server.Mcp/**/*.cs"]
---

# MCP tool conventions

Applies to the shared MCP tool layer (`src/Server.Mcp.Shared`) and the stdio CLI host
(`src/Server.Mcp`). The companion REST/HTTP MCP host lives in `src/Server.Api`.

Canonical contract: **`docs/SPEC.md`** (normative). When code and spec disagree, the spec
wins — cite sections (e.g. `SPEC §2.7`).

## Two hosts, one shared tool set

The same `[McpServerToolType]` tool classes run in both hosts; both register them through
`Server.Mcp.Shared.DependencyInjection.AddTools()`.

- **`Server.Mcp`** — stdio/CLI host. One SQL Server target supplied at startup (`-s <connstr>`
  or `-c <config>`), registered as the single data-source named `"main"`
  (`MainConfigurationFactory.MainDataSourceName`). Logs go to **stderr** (stdout is the
  JSON-RPC channel).
- **`Server.Api`** — HTTP MCP host (`MapMcp("/mcp")`, stateless). Multi-server, data-sources
  from `configs/main.json`.

Per-host behavior must come **only** from DI registration, never from forking the tool classes.
That is the entire point of the shared layer: two transports cannot drift into two contracts.
Both hosts are thin — they map Application-layer results onto their own surface (`CallToolResult`
here, ASP.NET Core results in the REST controllers) and share everything below.

The Aspire host orchestrates SQL Server and `Server.Api` only. The stdio server is **not** part of
it: stdio MCP is one-to-one, so the client (Claude Code, Claude Desktop, an integration test)
spawns the process itself.

## Structure — what / when / reason

```
src/Server.Mcp.Shared/
  DependencyInjection.cs          AddTools() + AddDefaultServerName(string?)
  Abstractions/                   layer-level contracts + shared helpers (not tied to one tool)
    IDefaultServerName.cs         ns: ssmsmcp.Server.Mcp.Shared.Abstractions
    ServerNameResolver.cs         per-host server_name resolution helper
  tools/                          concrete tool classes
    ServerTools.cs                ns: ssmsmcp.Server.Mcp.tools
    DatabaseTools.cs
    Abstractions/                 tool-agnostic result-shaping shared across tools
      ToolPayload.cs              ns: ssmsmcp.Server.Mcp.tools.Abstractions
      ServerInfoResult.cs         composite result payload record(s)
```

- **`tools/`** — _what:_ the concrete `[McpServerToolType]` classes (one tool method each).
  _When:_ a type is a tool. _Reason:_ keeps the wire-facing surface together and easy to audit.
- **`tools/Abstractions/`** — _what:_ reusable result-shaping used by more than one tool —
  the `CallToolResult` builder (`ToolPayload`) and result payload records (`ServerInfoResult`,
  and future `*Result` DTOs). _When:_ a type shapes tool output or is shared by ≥2 tools.
  _Reason:_ separates "how results are built" from "which tools exist".
- **`Abstractions/`** (layer root) — _what:_ contracts and helpers that are layer-wide, not
  tool-specific (`IDefaultServerName`, `ServerNameResolver`). _When:_ a contract or helper is
  consumed by DI/host wiring and several tools. _Reason:_ mirrors the repo's
  `Application/Abstractions`, `Domain/Abstractions` convention, and groups the resolver with the
  `IDefaultServerName` contract it depends on.

Namespaces mirror folders and include the `.Abstractions` segment (repo convention). Note the
tools namespace is `ssmsmcp.Server.Mcp.tools` (lowercase, and it drops the `.Shared` segment) —
follow it; do not "correct" it to `ssmsmcp.Server.Mcp.Shared.tools`.

Code in this layer is kept comment-free (no `//`, `///`, `/* */`) unless a comment is explicitly
requested.

## Authoring a tool

Per `SPEC §2.7`, `§2.8`, `§3.3`:

- **One tool method per spec tool.** Don't share method bodies "because they're similar";
  delegate shared logic to focused services/handlers instead. `describe_table` and
  `describe_procedure` look alike but have different validation, different SQL and different
  output schemas — keep them separate. Where two tools genuinely share logic (`script_object` /
  `script_objects`), share it in the layer below and keep both tool methods thin.
- **Tool classes are `internal sealed` with primary-constructor DI**, registered explicitly:
  `mcpBuilder.WithTools<ServerTools>()` in `Server.Mcp.Shared/DependencyInjection.cs`. Add new
  tool classes to `AddTools()`.
    > `deviation:` an earlier design note prescribed _static_ tool classes discovered by
    > `WithToolsFromAssembly()`. This repo uses instance classes with constructor injection and an
    > explicit registration list — the wire surface stays auditable in one place. Follow the code.
- **Set every annotation explicitly** — SDK defaults are unsafe for a read-only server:
  `[McpServerTool(Name = "snake_name", Title = "Human Title", ReadOnly = true,
Destructive = false, Idempotent = true, OpenWorld = false)]`. Set `Name` explicitly so the
  wire contract is auditable and method renames are safe.
- **snake_case parameter names** (`server_name`, `name_pattern`, `page`, `page_size`). The SDK
  uses the C# parameter name verbatim on the wire, so the identifier must be snake_case even
  though it clashes with C# style. `[Description]` every parameter (it is the only doc the LLM
  sees).
- **Return `CallToolResult`, never a raw object** — it is the only way to control `IsError`,
  `StructuredContent`, and the text fallback together. Build it with
  `ToolPayload.Structured(payload)`.
- **Never throw out of a tool method.** The SDK would replace the structured envelope with a
  generic message. Return an `isError` `CallToolResult` (e.g. `ToolPayload.MissingServerName()`).
- **Delegate to the Application layer** (Mediator requests/handlers). Tools are a thin transport
  layer — no SQL/business logic in them.
- Honor `CancellationToken` (auto-bound; excluded from the JSON schema; pass it down).
- **Prefer flat parameters** over binding the whole input as one args record. The SDK supports
  both, but per-parameter generation handles `[Description]` and C# default values (`= null`,
  `= false`) more cleanly.
- **Not yet implemented — `UseStructuredContent = true` + `OutputSchemaType = typeof(TResult)`.**
  Without them the SDK publishes no `outputSchema` and sends no `structuredContent`, which
  `SPEC §2.8` mandates. **No tool sets these today** (`ServerTools`, `DatabaseTools` both omit
  them) — `ToolPayload.Structured` populates `StructuredContent` by hand instead. Set both when
  the output-record types land.

### Result serialization

In `ToolPayload`:

- `CallToolResult.StructuredContent` is **`JsonElement?`**, not `JsonNode`. Use
  `JsonSerializer.SerializeToElement(...)`; `SerializeToNode`/`JsonNode` fails with **CS0029**.
- Serialize with **`ModelContextProtocol.McpJsonUtilities.DefaultOptions`** (the SDK's canonical
  options), not a bespoke `JsonSerializerOptions`, so input/output options stay aligned with the
  SDK.

> `deviation:` an earlier design note used `SerializeToNode` and a hand-built
> `JsonSerializerOptions` (snake_case policy + `JsonStringEnumConverter`) wired via
> `ConfigureOptions`. Both are wrong here — the first does not compile, the second desynchronizes
> input deserialization from output generation. The two rules above are what actually works.

### Output records

- `sealed record` with `required` / `init` members.
- `[JsonPropertyName("snake_case")]` per property where C# casing diverges from the wire. Explicit
  attributes are preferred over a global naming policy: enum values want UPPER_SNAKE_CASE while
  properties want lower_snake_case, and one policy cannot express both.
- **Inline the envelope fields** (`ok`, `tool`, `duration_ms`, `cached_at`, `warnings`) on each
  output record rather than wrapping payloads in a generic `Envelope<T>`. The wrapper produces
  `{"ok":…,"tool":…,"data":{…}}`, but the spec expects envelope fields as **siblings** of the
  payload fields. The duplication is deliberate and worth it for the correct wire shape.

## Error envelope — target state, not yet implemented

Today the only error path is `ToolPayload.MissingServerName()`. `SPEC §4` specifies a full
envelope; build it as additional `ToolPayload` members (`Success`, `Error`, `NotFound`,
`PermissionDenied`, `Cancelled`, `SqlError`, `Internal`) so no tool repeats envelope plumbing.

- Success: `ok`, `tool`, `duration_ms`, `cached_at`, `warnings` alongside the payload.
- Failure: `IsError = true`, structured
  `error { code, category, message, hint, retryable, sql_error }`, and the human-readable message
  in `content[0].text`.
- Map `SqlException.Number` to codes: `229`/`230`/`297`/`916` → `PERMISSION_DENIED`;
  `2812`/`2020`/`4902` → `OBJECT_NOT_FOUND`; otherwise `SQL_ERROR`. Further candidates worth
  handling: `15407` (login does not exist), `15151` (cannot find object). Strip the
  `Msg N, Level N, State N, …` prefix and return the human text plus the structured
  `sql_error` fields (number, state, class, line, procedure).
- **Never** put `SqlException.ToString()`, an exception message, or a stack trace in
  `error.message` — it leaks server internals and connection-string fragments. Log the full
  detail; return a friendly message.
- `McpException` is reserved for "you called this wrong" — argument problems the JSON schema
  cannot express (e.g. a malformed pagination token) — thrown with `McpErrorCode.InvalidParams`
  so it surfaces as a JSON-RPC error. A legitimate operation failure is never an exception;
  validate up front and return `Error("INVALID_PARAMETER", …)`.

## Progress — target state, not yet implemented

`SPEC §2.10` marks `script_objects`, `analyze_db_health` and deep `get_dependencies` walks as
progress-emitting. The SDK auto-binds `IProgress<ProgressNotificationValue>`; add it as a
parameter and call `progress.Report(new ProgressNotificationValue { Progress, Total, Message })`
as work completes. Safe to call unconditionally — when the client sent no `progressToken` the SDK
injects a no-op and the reports are swallowed.

## Logging

- **Stdout is the JSON-RPC channel.** Anything written to it corrupts the stream and the client
  disconnects. `Server.Mcp/Cli/McpCliHost.cs` already routes every level to stderr via
  `builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)`.
- Never log connection strings (scrub with `SqlConnectionStringBuilder` if one must be included)
  or full DDL bodies from scripting tools — large and potentially sensitive.
- Structured logs use `BeginScope` with `tool`, `database`, `principal`, `parameters_summary`,
  then a start and an end record carrying `ok` and `duration_ms`.
- Open choice: JSON console formatter (`AddJsonConsole`) in production, simple text in dev.

## Process lifecycle (stdio)

One process per client per session, spawned by the client and living across every tool call in
that session.

- **One unhandled exception kills the session** — the client only sees "MCP server crashed". The
  per-tool never-throw rule is the first line; keep a top-level try/catch around the host loop
  that logs to stderr and exits non-zero as the second.
- No state survives a session; each spawn starts cold. Fine for an admin server — design for it.
- ADO.NET pools per process, so the first call in a session pays the connection-open cost. Don't
  extend the pool lifetime to work around it.

## Per-host server resolution (`server_name`)

The instance model is single-target per process (`SPEC §2.4`), but the shared tools run in a
multi-server HTTP host too. Resolve, never hardcode:

- Every tool takes an **optional** `server_name` and resolves it via
  `ServerNameResolver.TryResolve(server_name, IDefaultServerName, out var resolved)`.
- **CLI/stdio** registers `services.AddDefaultServerName(MainConfigurationFactory.MainDataSourceName)`
  → omitting `server_name` resolves to `"main"`.
- **HTTP** registers `services.AddDefaultServerName(null)` → omitting `server_name` returns
  `ToolPayload.MissingServerName()` (the caller must name a data-source).

## Pagination

- Reuse the shared **`PagedResult<T>` / `PageRequest`** (`Application.Abstractions.Shared`) for
  list tools — a page-number model (`Page` 1-based, `PageSize` ≤ 100; use `Skip`/`Take`). Do
  **not** invent per-tool result/pagination types or cursor/token fields.
- The SDK's `PaginatedResult` / `NextCursor` paginate MCP **list primitives** (`tools/list`,
  `resources/list`), **not** `tools/call` results — do not use it for a tool's structured output.

> `deviation:` `SPEC` specifies opaque `page_token` cursors with `page_size` up to 1000. The
> implementation deliberately defers cursors to a later cross-cutting pass (alongside the result
> envelope and the CSV mirror — see `src/Application/Security/AGENTS.md`) and uses the
> page-number model meanwhile. Until that pass lands, follow the code: no token fields.

## Testing

Integration tests exercise both hosts (`tests/Server.Mcp.Integration` for stdio,
`tests/Server.Api.Integration` for HTTP) through a real MCP client over Aspire-hosted SQL Server.
Assert the exact `tools/list` set and call each tool; on the stdio host omit `server_name` (it
defaults to `main`), on the HTTP host pass `server_name` and assert that omitting it errors.

Two layers, both worth having:

1. **Protocol-level** — assert `tools/list` returns exactly the expected tools (22 per
   `docs/SPEC.md` v0.4) with the correct annotations on each (`readOnlyHint = true`,
   `destructiveHint = false`, `idempotentHint = true`, `openWorldHint = false`) and the expected
   `inputSchema` / `outputSchema` shapes. This layer needs no SQL Server.
2. **Behaviour-level** — end-to-end against seeded AdventureWorks: assert real payloads, not just
   that a call succeeded.

The stdio host is spawned as a child process with redirected stdin/stdout and its configuration
supplied via environment variables — the same way a real client launches it.

## SPEC → SDK quick reference

| Spec rule                    | Construct                                                                                                             |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `§1.2` capabilities          | Implicit — registering tools enables `tools`; declaring no resources/prompts leaves those capabilities absent.        |
| `§1.3` transports            | `.WithStdioServerTransport()` (`Server.Mcp`) / `MapMcp("/mcp")` (`Server.Api`).                                       |
| `§2.7` annotations           | `[McpServerTool(ReadOnly=true, Destructive=false, Idempotent=true, OpenWorld=false, Title="…")]` — all four explicit. |
| `§2.8` structured output     | `ToolPayload.Structured(payload)`; add `UseStructuredContent` + `OutputSchemaType` (pending).                         |
| `§2.9` cancellation          | `CancellationToken` parameter, auto-bound; propagate to every async call.                                             |
| `§2.10` progress             | `IProgress<ProgressNotificationValue>` parameter, auto-bound (pending).                                               |
| `§3.1` structured + text     | `CallToolResult { StructuredContent = …, Content = [new TextContentBlock { Text = … }] }`.                            |
| `§3.3` snake_case wire names | snake_case C# parameter identifiers; `[JsonPropertyName]` on record members.                                          |
| `§4.1` error envelope        | `CallToolResult { IsError = true, StructuredContent = …envelope… }` — never throw (pending).                          |
| `§5.4` envelope fields       | Inlined on each output record, not a generic wrapper.                                                                 |
| `§8.2` logs to stderr        | `LogToStandardErrorThreshold = LogLevel.Trace` in `AddConsole(...)`.                                                  |

## Things to NOT do

In rough order of how easily they happen by accident:

1. **Write to stdout in stdio mode.** Corrupts the JSON-RPC channel. Always stderr.
2. **Throw out of a tool method.** The SDK swaps your envelope for a generic message.
3. **Rely on `[McpServerTool]` annotation defaults.** `Destructive` and `OpenWorld` default to
   `false`, `ReadOnly` to `false` — all wrong for a read-only server. Set all four.
4. **Forget `UseStructuredContent = true`.** No output schema is published and no structured
   content is sent.
5. **Let C# PascalCase reach the wire.** Violates `SPEC §3.3`.
6. **Make the two hosts share tool method signatures.** They return different types. Share the
   layer _below_ them (`Server.Mcp.Shared` → Application), never the signatures.
7. **Hide a tool from `tools/list` because the target edition lacks the feature.** Keep it listed
   and return `UNSUPPORTED_FEATURE` — far easier to diagnose than a tool that silently vanishes.
