---
description: "SQL Server / SMO access guidelines"
paths: ["**/src/Infrastructure/**/*.cs", "**/src/Domain/Abstractions/**/*.cs"]
---

# SQL Server access conventions

Applies to the Infrastructure adapters (`src/Infrastructure/SSMS`) and the Domain ports they
implement (`src/Domain/Abstractions`). Canonical contract: **`docs/SPEC.md`** (normative).

## SMO first

The SSMS SDK (SMO) is the primary metadata API and `Microsoft.SqlServer.Management.Smo` types are
treated as the Domain model — see `src/Domain/AGENTS.md`. Object listing and description go
through SMO, not hand-rolled catalog queries.

Raw SQL over the existing SMO connection (`Server.ConnectionContext.ExecuteWithResults`) is
permitted **only** where SMO exposes no equivalent — e.g.
`sys.dm_exec_describe_first_result_set_for_object` for `describe_procedure`'s optional result-set
introspection. Prefer the object's numeric `object_id` over interpolating user input.

> `deviation:` an earlier design note argued the opposite ("use direct catalog queries; reserve
> SMO for `Scripter`"), written for a catalog-query-based architecture this repo did not adopt.
> The rule above is authoritative — do not re-open it.

## Connections

- Resolve servers through `IServerConnectionFactory.GetServer(serverName)`
  (`Abstractions/SSMS/`, impl in `SSMS/Internals/ServerConnectionFactory.cs`). Adapters are
  registered as singletons.
- Never cache a `SqlConnection` across calls — ADO.NET pooling already does this, and a cached
  connection outlives its pool lifetime. Open with `OpenAsync(ct)`, never synchronous `Open()`.
- Every `SqlCommand` uses the async overloads with the token: `ExecuteReaderAsync(ct)`,
  `ExecuteScalarAsync(ct)`, `ExecuteNonQueryAsync(ct)`. The synchronous variants block a thread
  and silently defeat cancellation.
- Entra ID / Managed Identity use `Authentication=Active Directory Default` (supported natively
  by `Microsoft.Data.SqlClient` v5+). `TrustServerCertificate` defaults to `false`.

## Identifier safety

Identifiers cannot be SQL parameters — they must be injected as text, which is exactly where
injection bugs come from. Two safe shapes:

```csharp
// Preferred: the identifier never reaches the SQL text.
cmd.Parameters.AddWithValue("@schema", schema);
cmd.Parameters.AddWithValue("@pattern", pattern);
// ... WHERE schema_id = SCHEMA_ID(@schema) AND name LIKE @pattern

// When interpolation is unavoidable: validate, then quote.
var safeDb = Identifiers.QuoteName(database);
var sql = $"SELECT * FROM {safeDb}.sys.objects WHERE ...";
```

A `QuoteName` helper wraps in `[...]` and escapes embedded `]` as `]]`. Reject any identifier
containing a NUL byte or exceeding 128 characters (the T-SQL limit). Never build an identifier by
plain string concatenation.

## DMVs

- Instance-level DMVs (`sys.dm_*`) require `VIEW SERVER STATE` on the connecting principal.
- **Never cache DMV output.** It is live state; staleness is worse than the round-trip.

## Caching

Current shape (`SSMS/DatabaseAdapter.cs` is the reference):

- `IMemoryCache`, 15-minute absolute expiration.
- Keys are scoped, e.g. `objects:{server}:{db}:{(long)types}`.
- `forceRefresh` evicts the key before re-populating.
- Surface the cache timestamp to the caller as `cached_at`.
- TTL is the **only** invalidation mechanism — there are no DDL-change notifications. The
  `force_refresh` flag is the escape hatch and is sufficient for interactive use.

## Cancellation

`SPEC §2.9` requires every tool to honor cancellation; the adapters are where the token actually
has to travel.

- Propagate `CancellationToken` into every async ADO.NET call.
- **SMO accepts no `CancellationToken`.** Wrap long SMO calls in
  `Task.Run(...).WaitAsync(ct)` — accepting that the SMO call itself completes in the background
  after cancellation. There is no way to abort it in-flight.
- For long-running DMV queries set `SqlCommand.CommandTimeout` below the cancellation budget; the
  timeout issues a server-side query cancel, which is the only reliable way to abort executing
  T-SQL.
- On cancellation return a cancelled result. **Never return partial results.**

## Target state — not yet implemented

Nothing below exists in the code today. Follow it when the corresponding tools are built.

### Dependency parsing (`get_dependencies`, `SPEC §7.7`)

Two information sources: `sys.sql_expression_dependencies` for what the engine recorded, and AST
parsing of `sys.sql_modules.definition` when the catalog returns an unresolved reference
(`is_ambiguous = 1` or `referenced_id IS NULL`).

For the AST path use `Microsoft.SqlServer.TransactSql.ScriptDom`: `TSql160Parser`
(SQL Server 2022; newer parsers are forward-compatible with older servers) plus a
`TSqlFragmentVisitor` subclass overriding `Visit(NamedTableReference)`, `Visit(FunctionCall)`,
`Visit(ProcedureReference)`. The parser returns a partial fragment plus an error list rather than
throwing — surface parse errors as **warnings**, never fail the whole call.

**Never regex over `sys.sql_modules.definition`.** It misreads CTE references, table-typed
parameters, table-valued functions and comments.

`sys.sql_expression_dependencies` is incomplete by design: it misses dynamic SQL, `OPENROWSET`,
and cross-server links. Report `is_ambiguous` and `unresolved_refs` honestly instead of implying
completeness.

### Scripting order (`script_objects`, `SPEC §7.10`)

Topologically sort by dependency (Kahn's algorithm over an in-degree map). Anything left
unordered is in a strongly-connected component — run Tarjan's algorithm to enumerate the SCCs for
the `cycle_paths` output. For objects in a cycle: emit `CREATE OR ALTER` stubs (original
signature, minimal body) first, then the full bodies in topological order within the SCC, and
attach the warning the spec mandates.

`script_object` and `script_objects` share substantial logic — share it in the scripting service
(`ScriptOneAsync` / `ScriptManyAsync`, the latter calling the former per object after sorting),
not by merging the two tool methods.

### Encrypted modules

`describe_procedure` against an `is_encrypted` procedure returns `body: null` plus a warning.
Do not throw, and do not return an empty string.

## Open choices

- **Catalog-query batching**: one round-trip per describe call, or several? A `describe_table`
  could fetch columns + indexes + FKs + constraints in a single batch with multiple result sets
  (faster, messier) or as separate queries (slower, clearer). Undecided.
- **ScriptDom parser version**: `TSql160Parser` covers SQL Server 2022; bump when targeting newer
  syntax.
