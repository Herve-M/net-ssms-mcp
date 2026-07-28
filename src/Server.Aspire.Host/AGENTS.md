# Server.Aspire.Host — AGENTS.md

The .NET Aspire orchestrator for local development and integration tests. It owns the SQL
Server containers, the HTTP host, and the MCP Inspector. Everything is declared in one file,
`AppHost.cs`.

## What it starts

- **Two SQL Server instances**, so behaviour can be checked across engine versions:
  `sql-2022` (port 1422) and `sql-2025` (port 1425). Both are built from
  `dockers/sql-{version}.dockerfile`, use `ContainerLifetime.Persistent` with a named data
  volume (`sql_data_22` / `sql_data_25`), and bind-mount `tests/data` read-only at
  `/var/opt/mssql/backup` so `dockers/scripts/restore-*.sql` can restore the AdventureWorks
  backups on first start. The `sa` password comes from the `sql-sa-password` parameter.
- **`http-api`** — the `Server.Api` project, referencing and waiting for both SQL instances.
- **`inspector`** — the MCP Inspector container (client on 6274, proxy on 6277), pointed at
  `http-api`.

`http-api` and `inspector` both use `.WithExplicitStart()` — they do not launch with the
AppHost; start them from the dashboard when needed.

## The stdio host is deliberately absent

`Server.Mcp` is **not** registered here, and that is not an oversight. A stdio MCP server is
one-to-one with its client: Claude Code, Claude Desktop or an integration test spawns the
process itself over stdin/stdout. Orchestrating it would have nothing to connect to. Do not
"fix" this by adding it.

## Security note

`DANGEROUSLY_OMIT_AUTH` is set on the Inspector **only** when
`builder.Environment.IsDevelopment()`. Keep that guard — the file carries the same warning
inline.
