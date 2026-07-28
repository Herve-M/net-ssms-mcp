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

<workspace-root>
│
├── src/  # main application code, organized into layers
│   ├── Server.Api/  # API server code
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Program.cs
│   │   └── ...
│   ├── Server.Mcp/  # MCP server host (stdio); composes layers and registers tools
│   │   ├── Program.cs
│   │   ├── README.md
│   │   └── ...
│   ├── Server.Mcp.Shared/  # shared MCP tool classes + DI, consumed by Server.Mcp (and Server.Api)
│   │   ├── tools/  # MCP tool classes (e.g. DatabaseTools.cs)
│   │   ├── DependencyInjection.cs
│   │   └── ...
│   ├── Server.Aspire.Host/  # Aspire orchestration host
│   │   ├── AppHost.cs
│   │   └── ...
│   ├── Application/  # application logic and services, divided into feature slice or domain boundaries
│   │   ├── DependencyInjection.cs
│   │   ├── Abstractions/  # shared interfaces and contracts
│   │   ├── AGENTS.md
│   │   └── ...
│   ├── Domain/  # domain models and business logic
│   │   ├── DependencyInjection.cs
│   │   ├── Abstractions/  # shared interfaces and contracts cross boundaries
│   │   ├── AGENTS.md
│   │   └── ...
│   ├── Infrastructure/  # data access, external services, infrastructure concerns
│   │   ├── DependencyInjection.cs
│   │   ├── Abstractions/  # shared interfaces, contracts, base classes
│   │   ├── SSMS/  # SQL Server / SSMS SDK client implementations/wrappers
│   │   ├── Configurations/  # DependencyInjectionBuilder definition
│   │   └── ...
├── tests/  # integration tests + shared fixtures
│   ├── Infrastructure.Integration/  # infrastructure integration tests
│   ├── Server.Api.Integration/  # API integration tests
│   ├── data/  # test fixtures (AdventureWorks .bak backups)
│   └── ...
├── docs/  # documentation directories
│   └── ...
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

LLM-optimized documentation lives in `AGENTS.md` files at the root of each layer (`Application`, `Domain`, `Infrastructure`). Each file describes that layer's purpose, its boundaries/feature-slices, and key types. Per-boundary `AGENTS.md` may be added as a layer grows.

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
