**Any code you commit SHOULD compile, and new and existing tests related to the change SHOULD pass.**

You MUST make your best effort to ensure your changes satisfy those criteria before committing. If for any reason you were unable to build or test the changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

## Context

- **Project Type**: Web API / MCP / CLI
- **Language**: C#
- **Framework**: .NET 10
- **Architecture**: Hexagonal / Clean-Architecture with FeatureSlice approach per Boundary, DDD, Event Based

### Project reference documents

This project has one canonical reference document that you MUST consult before any planning or implementation work:

- **`docs/SPEC.md`** — the tech-agnostic contract. Defines the 22 tools, their inputs and outputs (as JSON Schema), the error envelope, behavioral rules, serialization, and security posture. The spec is normative: if your code disagrees with the spec, the spec wins.

#### When to consult it

- **Before planning any feature or change**: read SPEC.md end-to-end.
- **Before writing or editing a tool, service, or test**: re-read the relevant SPEC section.
- **Before introducing any new pattern, package, or convention**: check that it does not contradict the spec. Surface any conflict and ask before deviating.

#### Quoting and citing

When referencing requirements in commits, PRs, or planning notes, cite the spec section (e.g., "per SPEC §2.7, every tool MUST explicitly set all four annotations"). This keeps decisions traceable.

## Structure

Every project below carries an `AGENTS.md` at its root (omitted from the tree for brevity — see
"Documentation near code").

<workspace-root>
│
├── src/  # main application code, organized into layers
│   ├── Server.Api/  # HTTP host: REST controllers + the HTTP MCP endpoint (MapMcp("/mcp"))
│   │   ├── Controllers/  # thin; delegate to Mediator
│   │   ├── Models/V1/  # versioned *Response records (REST contract)
│   │   ├── configs/  # main.json — the multi-server data-source list
│   │   ├── Program.cs
│   │   └── ...
│   ├── Server.Mcp/  # MCP server host (stdio); composes layers and registers tools
│   │   ├── Cli/  # McpCliHost (composition root) + MainConfigurationFactory
│   │   ├── RootCliCommand.cs  # --server/-s | --config/-c
│   │   ├── .mcp/  # client registration samples
│   │   ├── Program.cs
│   │   ├── README.md
│   │   └── ...
│   ├── Server.Mcp.Shared/  # shared MCP tool classes + DI, consumed by Server.Mcp and Server.Api
│   │   ├── tools/  # MCP tool classes (e.g. DatabaseTools.cs)
│   │   │   └── Abstractions/  # ToolPayload, result payload records
│   │   ├── Abstractions/  # IDefaultServerName, ServerNameResolver
│   │   ├── DependencyInjection.cs  # AddTools(), AddDefaultServerName()
│   │   └── ...
│   ├── Server.Aspire.Host/  # Aspire orchestration host (SQL 2022 + 2025, http-api, inspector)
│   │   ├── dockers/  # sql-*.dockerfile + restore scripts
│   │   ├── AppHost.cs
│   │   └── ...
│   ├── Application/  # application logic, divided into feature slices per boundary
│   │   ├── Databases/  # one folder per slice, e.g. GetDatabaseSchemas/
│   │   ├── Servers/
│   │   ├── Tables/ Views/ Procedures/  # the describe family
│   │   ├── Security/  # has its own AGENTS.md
│   │   ├── Abstractions/  # shared contracts; Shared/ holds PageRequest + PagedResult
│   │   ├── DependencyInjection.cs
│   │   └── ...
│   ├── Domain/  # ports and business contracts; SMO types are the domain model
│   │   ├── Abstractions/  # Databases/ Servers/ Security/ Configurations/ — the I…Port interfaces
│   │   ├── Configurations/  # MainConfiguration + validator
│   │   ├── DependencyInjection.cs
│   │   └── ...
│   ├── Infrastructure/  # data access, external services, infrastructure concerns
│   │   ├── SSMS/  # …Adapter per port; Internals/ holds the connection factory
│   │   ├── Abstractions/  # shared interfaces, contracts, base classes
│   │   ├── Configurations/  # DependencyInjectionBuilder definition
│   │   ├── DependencyInjection.cs
│   │   └── ...
├── tests/  # 3 integration suites + 1 unit suite
│   ├── Infrastructure.Integration/  # adapters vs real SQL Server (Testcontainers fixture)
│   ├── Server.Api.Integration/  # HTTP host end-to-end (Aspire fixture)
│   ├── Server.Mcp.Integration/  # stdio host end-to-end (Aspire fixture)
│   ├── Server.Mcp.Shared.Unit/  # the only unit project; no SQL Server
│   ├── data/  # test fixtures (AdventureWorks .bak backups)
│   └── ...
├── docs/  # documentation
│   └── SPEC.md  # the normative contract
├── eng/  # engineering rules
├── scripts/  # helper scripts for build, test, deploy
├── ssms-mcp.slnx  # solution file
├── global.json  # .NET global settings
└── ...

## Developer Workflow

- **Build the project**: `dotnet build ssms-mcp.slnx`

## Key Architectural Patterns

- **Clean/Hexagonal Architecture**: The solution is divided into `Domain`, `Application`, `Infrastructure`, and presentation (`Server.Api`, `Server.Mcp`, `Server.Aspire.Host`) layers.
    - `Domain`: Contains core business logic, entities, and interfaces.
    - `Application`: Contains application-specific logic, orchestrates domain services, and handles commands/queries (CQRS pattern with source-generated Mediator).
    - `Infrastructure`: Implements external concerns like SQL Server connectivity via the SSMS SDK, file systems, and clients for external services.
- **Dependency Injection**: Each project layer (`Application`, `Domain`, `Infrastructure`) has a `DependencyInjection.cs` file for registering its services. The main `Program.cs` in `Server.Api` and `Server.Mcp` composes these layers.
- **CQRS with Mediator**: Application logic is separated into Commands (actions that change state) and Queries (requests for data), using the source-generated `Mediator` package (`martinothamar/Mediator`). Controllers in `src/Server.Api/Controllers` delegate work to Mediator.
- **SQL Server Integration**: SQL Server connectivity is implemented in `src/Infrastructure/SSMS/` using the SSMS SDK, with the connection factory abstracted behind `IServerConnectionFactory`.
- `Microsoft.SqlServer.Management.Smo` is seen a Domain layer, and `src\Domain` it's extension if required.

## Documentation

### Documentation near code

LLM-optimized documentation lives in `AGENTS.md` files: one at the root of **every** `src/` project (`Application`, `Domain`, `Infrastructure`, `Server.Mcp.Shared`, `Server.Mcp`, `Server.Api`, `Server.Aspire.Host`) and one at `tests/`. Each describes that project's purpose, its boundaries/feature-slices, and key types.

A boundary earns its own nested `AGENTS.md` once it has **3+ feature slices or conventions that differ from its layer** — `src/Application/Security/AGENTS.md` is the worked example. Below that threshold it stays a section in the layer file.

`AGENTS.md` describes **structure** — what lives here, why, and which file is the reference implementation. `.claude/rules/` prescribes **conventions** — how to write the code. When both apply, `AGENTS.md` links to the rule instead of restating it, so the two cannot drift apart.

Template:

```markdown
# <Name> — AGENTS.md
<1–3 line purpose: what this is, what it depends on, what consumes it>

## Conventions          <- only what is NOT already in .claude/rules/; otherwise link
## Boundaries           <- folder by folder: what lives there and why
## Pattern (<name>)     <- the repeatable recipe, numbered
<closing line naming the reference implementation file>
```

Keep them current: when a boundary, port, adapter or tool is added, update the owning `AGENTS.md` in the same change.

### Instruction files

Task-specific guidelines live under `.claude/rules/`:

- [`rest-api.md`](.claude/rules/rest-api.md) — REST API conventions
- [`mcp.md`](.claude/rules/mcp.md) — MCP tool conventions
- [`sql-access.md`](.claude/rules/sql-access.md) — SQL Server / SMO access conventions
- [`integration-tests.md`](.claude/rules/integration-tests.md) — Integration testing

## Testing

### Integration testing

See [`integration-tests.md`](.claude/rules/integration-tests.md) on how to generate and update integration tests.

Integration tests run against an Aspire-hosted SQL Server seeded with AdventureWorks; the stdio MCP host is spawned as a child process the way a real client launches it. Per-surface detail lives in [`mcp.md`](.claude/rules/mcp.md) and [`rest-api.md`](.claude/rules/rest-api.md).

## Requirements

### MUST

- NuGet prefixed with `Microsoft.` MUST follow .NET 10 versioning
- NuGet versions MUST be declared centrally in `Directory.Packages.props` (CPM); project files carry `PackageReference` without a `Version`
- `ModelContextProtocol.AspNetCore` belongs to `Server.Api` only — it is the HTTP-transport package and MUST NOT be referenced by the stdio host `Server.Mcp`
- `ModelContextProtocol.Core` MUST NOT be referenced directly; the parent `ModelContextProtocol` package brings it in along with hosting, DI and attribute discovery
- Secrets MUST be sourced from a secret store via a `*_secret_ref` URI (`env:`, `keyvault://`, `dpapi:`); a plaintext password field MUST NOT be accepted in configuration
- `TrustServerCertificate` MUST default to `false` — it is a per-environment override, never a baseline
- Configuration MUST be validated at startup and the process MUST exit non-zero before serving if invalid — see `MainConfigurationValidator` and `OptionsValidators<T>` in `src/Domain`
- `eng` msbuild `.props` files MUST not be modified and MUST be followed
- Project `Application`, `Domain`, `Infrastructure` MUST have a `DependencyInjection.cs` used to register classes in the DI container, following Clean Architecture principles.
- Use `ILogger<T>` for logging.
- Use Dependency Injection
- Use Clean Architecture with layered separation, but using Hexagonal naming/vision.
- Markdown lint error MD033 is ignored for .github/\*_/_.md files

### SHOULD

- Use MCP tools called `microsoft_docs_search` and `microsoft_docs_fetch` search through and fetch Microsoft's latest official documentation
  - see `microsoft-docs` and `microsoft-code-reference` skills
- Use the SSMS SDK (SMO) for SQL Server metadata access. Raw SQL execution via the existing SMO connection (`Server.ConnectionContext.ExecuteWithResults`) is exceptionally permitted only when SMO provides no equivalent API for the requested data — e.g. `sys.dm_exec_describe_first_result_set_for_object` for `describe_procedure`'s optional result-set introspection. Prefer the object's numeric `object_id` (not string interpolation of user input) when such a call is unavoidable.

### COULD

### SHOULD NOT

- Use STS .NET Release and Features
- Use `MassTransit`, `FluentAssertions`, `AutoMapper`, `MediatR` NuGet and code
- Log connection strings or full DDL bodies — scrub connection strings via `SqlConnectionStringBuilder`; DDL bodies are large and potentially sensitive
- Put raw exception text, `Exception.ToString()`, or stack traces in any client-visible message — log the detail, return a friendly message

### References

- [`.editorconfig`](.editorconfig)
- [C# Coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)
- [Common C# guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- https://herbertograca.com/2017/11/16/explicit-architecture-01-ddd-hexagonal-onion-clean-cqrs-how-i-put-it-all-together/
