# Application layer — AGENTS.md

Application-specific logic: orchestrates the Domain, exposes use cases as CQRS
queries/commands. Depends on `Domain`; consumed by `Server.Api` and
`Server.Mcp.Shared`. No SQL/SMO connection code here — that's Infrastructure.

## Conventions

- **CQRS via source-generated Mediator** (`martinothamar/Mediator`, namespace `Mediator`).
  Do **not** use `MediatR`.
- One feature slice = one folder under a boundary, containing a single `*.cs` file that
  declares the **request record**, its **DTO record(s)**, and the **handler** together.
- Requests implement `IRequest<TResponse>`; handlers implement
  `IRequestHandler<TRequest, TResponse>` with `ValueTask<TResponse> Handle(...)`.
- Handlers take dependencies via primary-constructor injection
  (`ILogger<THandler>` + the Domain port they need).
- DTOs are `sealed record` with `required`/`init` members. Handlers map SMO types →
  DTOs; SMO types never leave the Application layer.
- Register via `DependencyInjection.cs` (`UseApplicationLayer`), which wires the
  Mediator source generator over this assembly.

## Boundaries

- **`Databases/`** — per-database metadata queries. Paginated collection queries
  (`GetDatabaseSchemas`, `GetDatabaseTables`, `GetDatabaseViews`,
  `GetDatabaseStoredProcedures`, `GetDatabaseUserDefinedFunctions`,
  `GetDatabaseUserDefinedTypes`, `GetDatabaseUserDefinedTableTypes`,
  `GetDatabaseUsers`, `GetDatabaseTriggers`, `GetDatabaseRoles`) return
  `PagedResult<TDto>`. Single-object queries (`GetDatabaseDetails`,
  `GetDatabaseConfiguration`, `GetDatabaseStorage`, `GetDatabaseSecurity`,
  `GetDatabaseAvailability`, `GetDatabaseFeatures`) return a single DTO. Backed by
  the `Domain.Abstractions.Databases` ports.
  - **`GetDatabaseObjects`** — generic object listing (tables, views, procedures,
    functions, synonyms, sequences, types, XML schema collections, service queues).
    Also returns `PagedResult<DatabaseObjectDto>`, but follows the **in-memory** pattern
    below rather than the port-`Count` pattern: it fetches the full cached object set
    once and applies type/schema/name filtering, sorting, and pagination in the handler.
    Maps each row to a SQL-style `type_desc` and a delimited 3-part `fqn`
    (`[db].[schema].[name]`, brackets only when an identifier isn't `[A-Za-z_][A-Za-z0-9_]*`,
    per SPEC §2.5).
- **`Servers/`** — server-level queries (`GetServerOverview`, `GetServerEngine`,
  `GetServerVersion`, `GetServerStorage`, `GetServerSecurity`, `GetServerCapacity`,
  `GetServerConnectivity`, `GetServerFeatures`, `GetServerPlatform`,
  `GetServerLocalization`, `GetServerIdentity`, `GetServerAvailability`,
  `GetServerDatabases`, `GetServersList`). Backed by `IServerPort`.
- **`Tables/`**, **`Views/`**, **`Procedures/`** — the describe family, one slice each
  (`DescribeTable`, `DescribeView`, `DescribeProcedure`). See the section below.
- **`Security/`** — read-only security surface (`ListPrincipals`, `ListRoleMemberships`,
  `ListPermissions`). Has its own [`AGENTS.md`](Security/AGENTS.md).

## Describe slices

`Tables/DescribeTable`, `Views/DescribeView` and `Procedures/DescribeProcedure` are one
family and differ from every other boundary here:

- Each returns a **single nullable composite DTO** (`DescribeTableDto?`, `DescribeViewDto?`,
  `DescribeProcedureDto?`) — never a `PagedResult`. `null` means the object does not exist;
  the handler does not throw.
- They compose several sub-DTOs into one payload rather than mapping one SMO type flat.
- They share mapping code in `Abstractions/TableViewMappers`: `MapColumns`, `MapIndexes`,
  `MapTriggers`, `MapStatistics`, `FormatDataType`.

**Watch the DTO ownership.** `ColumnDto`, `IndexDto`, `IndexKeyColumnDto`, `TriggerDto`,
`StatisticDto`, `PrimaryKeyDto` and `CheckConstraintDto` are declared inside
`Tables/DescribeTable/DescribeTable.cs` (namespace `ssmsmcp.Application.Tables`) but are
consumed by `Views`, `Procedures` and `Abstractions/TableViewMappers`. So the `Tables`
boundary owns the shared column/index vocabulary — an exception to "one slice = one
self-contained file". Editing those records affects three boundaries; check all three.

Genuinely cross-boundary describe records live in `Abstractions/` instead: `ObjectRefDto`,
`ParameterDto`, `ForeignKeyRowDto`.

## Shared (`Abstractions/Shared/`)

- **`PageRequest`** — `Page` (1-based, default 1), `PageSize` (default 20, max 100),
  optional `SortBy`/`SortDescending`; exposes `Skip`/`Take`. `Validate()` throws
  `ArgumentOutOfRangeException` on invalid input — call it at the top of paginated
  handlers.
- **`PagedResult<T>`** — `Items`, `TotalCount`, `Page`, `PageSize`, plus computed
  `TotalPages`/`HasNextPage`/`HasPreviousPage`. Build with
  `PagedResult<T>.Create(items, totalCount, page, pageSize)`.

The `Abstractions/` root (not `Shared/`) additionally holds:

- **`TableViewMappers`**, **`ObjectRefDto`**, **`ParameterDto`**, **`ForeignKeyRowDto`** —
  the describe family's shared mapping and records.
- **`Identifiers`** — `BuildFqn`, `BuildQualifiedName`, `Quote`: builds the delimited
  display FQN (`[db].[schema].[name]`, brackets only when an identifier isn't
  `[A-Za-z_][A-Za-z0-9_]*`) per SPEC §2.5. This is **presentation only** — it is not a SQL
  injection guard; see [`sql-access.md`](../../.claude/rules/sql-access.md).

## Pattern (paginated query)

1. `request.Pagination.Validate()`.
2. `await port.Get…Count(...)` for the total.
3. `await port.Get…(serverName, databaseName, Skip, Take, ct)` for the page.
4. Map SMO → DTO with LINQ `Select`.
5. Return `PagedResult<TDto>.Create(...)`.

See `Databases/GetDatabaseSchemas/GetDatabaseSchemas.cs` as the reference implementation.

## Pattern (in-memory filter/page)

For enumeration whose port returns the full set (no server-side paging — see
`IDatabasePort.GetDatabaseObjects`):

1. `request.Pagination.Validate()`.
2. `await port.GetDatabaseObjects(server, db, SupportedTypes, request.ForceRefresh, ct)` —
   one cached enumeration of the supported-type union.
3. Filter in memory with `OrdinalIgnoreCase` (type, then system schema, then exact
   schema, then substring name).
4. `OrderBy` schema then name, `Skip`/`Take` the page, map → DTO.
5. Return `PagedResult<TDto>.Create(...)` with the **filtered** count.

See `Databases/GetDatabaseObjects/GetDatabaseObjects.cs`.

## Pattern (describe)

For single-object introspection:

1. Resolve the object via the typed port's lookup overload
   (`port.Get…(server, db, schema, name, ct)`), which returns a nullable SMO type.
2. Return `null` immediately when it is absent — do not throw.
3. Compose the sub-DTOs with `TableViewMappers` helpers plus any slice-specific mapping.
4. Set `Fqn` with `Identifiers.BuildFqn(database, schema, name)`.
5. Return the composite `Describe…Dto`.

See `Tables/DescribeTable/DescribeTable.cs` as the reference implementation.
