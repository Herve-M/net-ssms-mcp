# tests — AGENTS.md

Four test projects. Three are integration suites that need a real SQL Server; one is a unit
suite. Naming, folder mirroring and coverage rules live in
[`integration-tests.md`](../.claude/rules/integration-tests.md) — not repeated here.

## Projects

- **`Infrastructure.Integration/`** — the SMO adapters against a real server.
  `SSMS/*AdapterTests.cs` mirrors `src/Infrastructure/SSMS/`.
- **`Server.Api.Integration/`** — the HTTP host end to end. `Tools/` drives the MCP endpoint;
  `server_name` is required here.
- **`Server.Mcp.Integration/`** — the stdio host end to end, through a real MCP client over a
  spawned child process. `Tools/` per tool family, `Cli/` for the CLI surface. Omitting
  `server_name` must resolve to `"main"`.
- **`Server.Mcp.Shared.Unit/`** — the only unit project; no SQL Server. Currently
  `Abstractions/ServerNameResolverTests.cs`.

## Two container strategies — know which one you are in

They are not interchangeable; pick the fixture that matches the project you are extending.

- **`Infrastructure.Integration` uses Testcontainers directly.**
  `Fixtures/SqlServerFixture` builds images from
  `src/Server.Aspire.Host/dockers/sql-*.dockerfile` and runs them itself, keyed by
  `SqlServerVersion` / `SqlServerImageSpec` (`Sql2022`, `Sql2025`, and `All` to iterate both).
  Registered once via `Fixtures/AssemblyFixtures` and shared across every test class.
- **`Server.Api.Integration` and `Server.Mcp.Integration` use Aspire.**
  `Fixtures/AspireContext` spins up the real AppHost with
  `DistributedApplicationTestingBuilder`, exposing `Sql2022Resource` / `Sql2025Resource`,
  `EnsureStartedAsync`, `WaitForSqlAsync` and `GetStdioMcpClientAsync`.

Either way the suites run against **both** engine versions — a test that passes on 2022 and
fails on 2025 is a real finding, not flakiness.

## Data

`data/` holds the AdventureWorksLT backups (`2019`, `2022`, `2025` `.bak`) restored into the
containers on first start; `license.txt` and `README.md` cover their provenance. The directory
is bind-mounted read-only — tests must never write to it.
