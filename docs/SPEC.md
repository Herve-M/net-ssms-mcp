# SQL Server Administrative MCP — Specification (v0.4)

> **Status:** Draft v0.4 — Read-only administrative surface.
> **Audience:** Implementers and AI coding assistants (Claude Code et al.) generating or maintaining server code from this contract.
> **Scope:** Tool contracts, behavioral rules, serialization, error model, security posture. Tech-agnostic; no implementation guidance.
> **Non-scope:** Any write/mutation capability (deferred indefinitely), transport details beyond what MCP itself specifies, choice of language/runtime/SQL parser/scripting library.

**Changes from v0.3**

- Security tool surface refactored to fix the output-size problem in the v0.3 `list_principals`. The previous shape (six named arrays on one result object) returned tens of KB even for empty SQL Server installs and hundreds of KB on realistic servers, on *every* call regardless of what the caller asked. Replaced with three narrower tools, each with a single well-defined output shape and proper pagination:
  - **`list_principals`** (§7.17, redesigned) — flat, paginated list of principals. Items carry a `principal_kind` discriminator (`SERVER_LOGIN` / `DATABASE_USER` / `SERVER_ROLE` / `DATABASE_ROLE`). Backed by `sys.server_principals` and `sys.database_principals`, both of which mix logins/users with roles in the same catalog view — matching that shape in the tool output is honest.
  - **`list_role_memberships`** (§7.18, new) — role → member edges, with optional transitive expansion for nested role chains.
  - **`find_orphaned_users`** (§7.19, new) — database users whose SID no longer maps to a server login. Diagnostic tool, no pagination.
- Tool count is now **22** (was 20 in v0.3).
- `list_permissions`, `list_jobs`, `list_backups` renumbered to §7.20, §7.21, §7.22 respectively. No content changes to those tools.
- Appendix A (quick reference), Appendix B (annotation matrix and titles), §6 (tool catalog), and §9 (conformance levels) updated to reflect the new tool count and numbering.

**Changes from v0.2**

- `describe_procedure` (§7.5) refined to properly distinguish T-SQL from CLR (Common Language Runtime, i.e. compiled .NET) procedures and functions:
  - The `kind` enum now separates T-SQL and CLR variants across all shapes: `PROCEDURE` / `CLR_PROCEDURE`, `SCALAR_FUNCTION` / `CLR_SCALAR_FUNCTION`, `TABLE_FUNCTION` / `CLR_TABLE_FUNCTION`. `INLINE_TABLE_FUNCTION` remains T-SQL-only (SQL Server has no CLR equivalent). `AGGREGATE_FUNCTION` remains a single value (always CLR).
  - New optional `clr` sub-object in the output, populated only for CLR objects. Contains `assembly_name`, `assembly_class`, `assembly_method`, `assembly_id`, and `execution_context_principal`. Sourced from `sys.assembly_modules` joined with `sys.assemblies`.
  - Behavioral notes updated: for CLR objects, `body` MUST be `null` (there is no T-SQL body), `clr` MUST be populated, and a warning MUST be emitted. `first_result_set_columns` MAY be `null` for CLR TVFs even when explicitly requested.
- No changes to `list_objects` (§7.3). Its `object_type` filter values remain user-intent taxonomy (`PROCEDURE`, `FUNCTION`), covering both T-SQL and CLR variants; per-row output preserves SQL Server-specific `type_desc` for downstream discrimination.

**Changes from v0.1**

- Free-form SQL execution (`execute_sql`) removed. The server is now read-only by construction: all SQL is server-authored from system catalogs/DMVs. Tool count is **20**, not 21.
- MCP protocol version target updated to `2025-11-25` (current). Floor remains `2025-06-18` for structured output and elicitation.
- Read-only enforcement model simplified from three layers to two (connection-level least privilege + server-authored SQL only). Removed `READONLY_VIOLATION` from the error code enum.
- New behavioral rules added for **cancellation propagation** and **progress notifications** (§2.9, §2.10).
- Tool-annotation guidance hardened: every tool MUST explicitly declare all four annotations (`readOnlyHint`, `destructiveHint`, `idempotentHint`, `openWorldHint`). Default behavior in some toolkits is *unsafe-by-default*; explicit declaration is required for conformance.
- New §1.4 on stateful vs stateless operation, primarily relevant for HTTP transport.

---

## 0. Conventions of this document

- **MUST / SHOULD / MAY** follow RFC 2119 semantics. **MUST NOT** is a hard prohibition; an implementation that violates a MUST is not conformant.
- **JSON Schema** fragments are illustrative and use draft 2020-12 conventions. They define the *contract*, not a wire format choice (see §3 on serialization).
- **Fully qualified name (FQN)** in this document means the SQL Server-style 3-part name `[database].[schema].[name]`. A 4-part name including server is allowed in cross-server contexts but the server component is informational only — the MCP does not establish remote connections on the user's behalf.
- **Object identity** in tool inputs and outputs uses FQN strings AND the SQL Server numeric `object_id` where applicable. Inputs accept FQN or 2-part names (`schema.name`, with `database` defaulting per §2.4); outputs MUST always include both FQN and `object_id`.
- **Snake_case** is the canonical naming style for tools, parameters, and JSON field names.

---

## 1. Server identity and capabilities

### 1.1 Server name and protocol

The server MUST advertise itself with:

- A stable `name` chosen by the implementation (suggested form: `mssql-admin`).
- A semantic `version` string.
- MCP protocol version compliance with at least `2025-11-25` (the current spec at the time of this document). `2025-06-18` is the floor (structured tool output, output schemas, elicitation, resource indicators).

### 1.2 Capabilities declared

The server MUST declare:

- `tools` capability with `listChanged: false` (the v0 tool set is static per session).

The server MUST NOT declare in v0:

- `resources` capability.
- `prompts` capability.
- `sampling` capability.
- `elicitation` capability (although the underlying mechanism may be available — see §11 v1 candidates).

Rationale: client support for resources and prompts is uneven across MCP clients; v0 is a Tools-only surface to maximize compatibility.

### 1.3 Transport

The server MUST support `stdio`. It MAY additionally support Streamable HTTP per MCP `2025-03-26`+. SSE-only transport is non-conformant. When Streamable HTTP is offered, the server SHOULD prefer stateless mode (see §1.4) unless a future capability requires statefulness.

For HTTP transport, the server MUST:

- Validate the HTTP `Host` header against a configured allow-list, to defeat DNS-rebinding attacks against local servers.
- Default to no CORS. Enable CORS only on explicit, narrow configuration (specific allowed origins).
- Require TLS for any non-loopback host.

### 1.4 Stateful vs stateless operation

The v0 server MAY operate in stateless mode and is RECOMMENDED to do so for HTTP transport. Stateless operation implies:

- No unsolicited notifications from the server (including tool list change notifications — already disabled per §1.2).
- No server-initiated sampling or elicitation requests.

Stateful operation is permitted but adds no v0 functionality and incurs operational complexity (session affinity, connection tracking). Implementations that anticipate v1 features (e.g., elicitation for scripting choices) MAY operate in stateful mode from the start.

---

## 2. Behavioral rules (apply to all tools unless overridden)

### 2.1 Read-only enforcement

The server is **read-only by construction**. All SQL executed against the target instance is server-authored (built from system catalog views, DMVs, and template scripting). User-supplied free-form SQL is **out of scope** for v0 (see §10).

Read-only enforcement is built on two independent layers:

1. **Connection-layer constraint** — the database principal used by the server MUST have only the minimum permissions required to read system catalog views, DMVs, msdb job tables, and (where applicable) object definitions. The principal MUST NOT hold write permissions on any user data or DDL/DCL permissions on any object. Recommended bare-minimum grants: `VIEW SERVER STATE`, `VIEW ANY DEFINITION`, `VIEW ANY DATABASE`, `SELECT` on the relevant `msdb` system tables for job/backup history, and `CONNECT` on each target database. The principal MUST NOT be `sysadmin`, `dbcreator`, `securityadmin`, `serveradmin`, `setupadmin`, `processadmin`, or `bulkadmin`.
2. **Server design constraint** — every SQL statement issued by the server MUST be authored by the server itself, parameterized over user inputs that are bound as typed parameters (never string-interpolated). No tool MAY accept free-form SQL as input.

The server SHOULD additionally enforce a connection string property of `ApplicationIntent=ReadOnly` when a read-only AlwaysOn replica is configured (defense in depth; not required when the principal already lacks write grants).

### 2.2 Pagination

Every tool whose result is a list of objects (any tool whose name begins with `list_` or returns a `rows` / `objects` / `paths` array) MUST support pagination via:

- A `page_size` input parameter (integer, default `100`, max `1000`).
- A `page_token` input parameter (opaque string; null/absent on first call).
- A `next_page_token` field in the output (null when no more results).
- A `total_count` field in the output where computing it is cheap; otherwise the field MAY be omitted and the LLM relies on `next_page_token`.

This is application-level pagination distinct from any MCP protocol-level cursor on `tools/list`.

### 2.3 Result-set bounds

For tools returning tabular data (performance and health tools), every output MUST be bounded by:

- A `row_limit` input parameter (default `500`, max `10000`).
- A `truncated` boolean field in the output, set `true` if the underlying result exceeded `row_limit`.

The server MUST NOT return more rows than `row_limit`. If `truncated` is true, the LLM is expected to either narrow the query or paginate.

### 2.4 Database scope (single-instance, multi-database)

The server connects to a single SQL Server instance per process. Within that instance:

- Every tool that operates on a specific database MUST accept an optional `database` parameter (string).
- If `database` is omitted, the server uses a configured default database (set at process start) or, if no default is configured, the principal's default database.
- The server MUST NOT silently switch the connection's current database; it MUST scope queries by 3-part naming or by `USE` semantics that are confined to the tool call.

### 2.5 Identifier handling

- Inputs accepting object names MUST accept both delimited (`[my schema].[my table]`) and undelimited (`dbo.MyTable`) forms.
- The server MUST handle case sensitivity per the database's collation; FQN comparisons MUST follow the target database's collation rules.
- Outputs MUST always emit identifiers in unambiguous delimited form when the identifier contains anything other than `[A-Za-z_][A-Za-z0-9_]*`.

### 2.6 Schema/metadata caching

The server MAY cache schema metadata (table lists, column lists, index lists) for a bounded TTL. Tools MUST accept a `force_refresh` boolean parameter (default `false`) that bypasses the cache for that call. If caching is implemented, results from a cached read MUST include `cached_at` (ISO-8601 timestamp) in their output envelope.

### 2.7 Tool annotations

Every tool MUST declare all four MCP tool annotations explicitly. The full set used in v0:

```json
{
  "title": "Human-Readable Tool Title",
  "readOnlyHint": true,
  "destructiveHint": false,
  "idempotentHint": true,
  "openWorldHint": false
}
```

`title` MUST be set (suggested values in Appendix B). The four boolean hints are normative for v0 and MUST match the values above on every tool. Implementations MUST NOT rely on framework defaults for these fields — some frameworks default to `destructiveHint: true` and `openWorldHint: true`, which would misrepresent this server's read-only nature. Each tool MUST explicitly set the values.

Tools MAY additionally declare an `icons` array per the MCP spec. This is OPTIONAL in v0.

### 2.8 Structured tool output

Every tool MUST declare an `outputSchema` (JSON Schema 2020-12) per MCP `2025-06-18`+. Tools MUST return their result in both forms on every call:

- `structuredContent` matching the declared `outputSchema`.
- A `content` block containing a text representation, for clients that do not yet consume `structuredContent` natively.

For tools whose output naturally renders as tabular CSV (per §3.1), the text representation in `content` is the CSV; the `structuredContent` mirrors the same rows as a `{columns, rows}` JSON pair.

### 2.9 Cancellation

Every tool MUST honor cancellation propagated from the client per the MCP cancellation flow. Implementations MUST:

- Accept and react to a cancellation signal at every awaitable boundary (network I/O, database round-trip, long-running scripting loops).
- Abort the underlying SQL execution as quickly as practical when cancellation arrives (e.g., issue an `KILL` of the executing request from a separate connection, or use ADO.NET-level cancellation if available).
- NOT return partial structured output on cancellation; the response on a cancelled call MUST follow the error envelope with `error.code = "CANCELLED"` (see §4.2).

### 2.10 Progress notifications

Tools whose typical execution exceeds 2 seconds against a healthy target SHOULD emit progress notifications when invoked with a client-supplied progress token. Tools that SHOULD support progress in v0:

- `script_objects` (per-object increments).
- `analyze_db_health` (per-check increments).
- `get_dependencies` with `max_depth >= 5` or `direction = "BOTH"` (per-level increments).

The progress payload structure follows the MCP spec's `ProgressNotification`. Tools that do not have a natural progress dimension MAY omit progress reporting; the lack of progress notifications MUST NOT be considered a conformance failure for tools outside the list above.

---

## 3. Serialization rules

### 3.1 Default format by data shape

The server MUST select serialization based on the response shape:

| Shape                                                                              | Format                                                                                                    | Where                |
| ---------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- | -------------------- |
| Typed structured object (`describe_table`, `get_server_info`, `analyze_db_health`) | JSON in `structuredContent` + JSON pretty-print in `content[0].text`                                      | All structured tools |
| Wide tabular (`get_top_queries`, performance lists)                                | CSV inside a fenced code block in `content[0].text`; JSON `{columns, rows}` mirror in `structuredContent` | All tabular tools    |
| DDL scripts (`script_object`, `script_objects`)                                    | Plain text inside `content[0].text`; metadata envelope in `structuredContent`                             | Scripting tools      |
| Errors                                                                             | JSON `error` envelope in `structuredContent`; human message in `content[0].text` (see §4)                 | All tools on failure |

### 3.2 CSV conventions

- RFC 4180 quoting.
- UTF-8, no BOM.
- Header row mandatory.
- `NULL` represented as the literal four-character string `NULL` (unquoted); empty string represented as `""` (quoted empty). The output schema MUST document this convention so the LLM can disambiguate.
- Date/time values formatted as ISO-8601.
- Binary (`varbinary`, `image`) values rendered as `0x` + hex, truncated to 64 hex chars with a trailing `…(truncated, N bytes total)` if longer.

### 3.3 JSON conventions

- UTF-8.
- Field names in `snake_case`.
- Enum values are strings in `UPPER_SNAKE_CASE` when they mirror SQL Server `type_desc` (`USER_TABLE`, `VIEW`, `SQL_STORED_PROCEDURE`, …) and `lower_snake_case` for server-defined enums.
- Timestamps as ISO-8601 strings with explicit timezone offset (`Z` for UTC).
- `null` is preferred to the absence of a field when a field is part of the schema and not currently populated.

### 3.4 No TOON in v0

The server MUST NOT emit Token-Oriented Object Notation (TOON) or any similar non-JSON structured format from tool responses in v0. CSV is permitted only for tabular shapes per §3.1. Implementations MAY add a future `format` parameter to opt into alternative serializations, but its absence is the conformant default.

---

## 4. Error model

### 4.1 Error envelope

Errors MUST be returned as a tool result with `isError: true`, NOT as a JSON-RPC protocol error, unless the error is a transport-level or schema-validation issue that prevents the tool from running at all. Implementations that surface errors only via raw exceptions risk having the exception text replaced with a generic message by some frameworks; conformant servers MUST return a structured error envelope explicitly.

The error envelope in `structuredContent`:

```json
{
  "error": {
    "code": "string (enum, see §4.2)",
    "message": "string (human-readable, suitable for LLM consumption)",
    "category": "string (enum: 'permission' | 'validation' | 'not_found' | 'sql' | 'timeout' | 'cancelled' | 'internal')",
    "sql_error": {
      "number": 0,
      "state": 0,
      "severity": 0,
      "line": 0,
      "procedure": null
    },
    "hint": "string (optional, actionable suggestion for the caller)",
    "retryable": false
  }
}
```

The `sql_error` object is REQUIRED when `category == "sql"` and MUST be omitted otherwise.

The `content[0].text` block on an error MUST contain the `error.message` so clients that do not yet read `structuredContent` still see a useful failure description.

### 4.2 Error codes

The following codes are normative for v0:

| Code                  | Category   | Meaning                                                                                                                             |
| --------------------- | ---------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `INVALID_PARAMETER`   | validation | Tool input failed schema validation or semantic checks.                                                                             |
| `OBJECT_NOT_FOUND`    | not_found  | Requested database/schema/object does not exist.                                                                                    |
| `PERMISSION_DENIED`   | permission | The MCP's database principal lacks permissions on the target.                                                                       |
| `SQL_ERROR`           | sql        | The database returned an error during execution.                                                                                    |
| `RESULT_TOO_LARGE`    | validation | A non-paginated response would exceed configured limits even after `row_limit`.                                                     |
| `TIMEOUT`             | timeout    | The query exceeded the server's configured execution timeout.                                                                       |
| `CANCELLED`           | cancelled  | The client cancelled the call before it completed.                                                                                  |
| `AMBIGUOUS_OBJECT`    | validation | An identifier resolved to more than one object (e.g., 2-part name in a multi-database call).                                        |
| `UNSUPPORTED_FEATURE` | validation | The target SQL Server version/edition does not support the requested capability (e.g., AlwaysOn on Express, Agent on Azure SQL DB). |
| `INTERNAL_ERROR`      | internal   | Unexpected server error. The server MUST NOT leak stack traces or connection strings.                                               |

(`READONLY_VIOLATION` was present in v0.1 and is removed in v0.2 since the server no longer accepts free-form SQL input.)

### 4.3 Error message rules

- Messages MUST be in English.
- Messages MUST NOT echo back full SQL the user submitted (irrelevant in v0 since user-submitted SQL is no longer accepted, but the rule stands for any future write capability).
- For `PERMISSION_DENIED`, the message MUST name the missing permission (e.g., `VIEW DEFINITION`, `VIEW SERVER STATE`) when the underlying SQL Server error makes this knowable.
- For `SQL_ERROR`, `sql_error.line` SHOULD be populated when the error refers to a parseable script fragment (e.g., when scripting an object's definition triggers a parse-time error).
- For `CANCELLED`, the message SHOULD note whether the cancellation arrived during query execution or during result serialization.

---

## 5. Common input/output fragments

These are reused across tool schemas; defined here to avoid repetition.

### 5.1 Pagination fragments

```json
{
  "$defs": {
    "pagination_input": {
      "type": "object",
      "properties": {
        "page_size": {"type": "integer", "minimum": 1, "maximum": 1000, "default": 100},
        "page_token": {"type": ["string", "null"], "default": null}
      }
    },
    "pagination_output": {
      "type": "object",
      "properties": {
        "next_page_token": {"type": ["string", "null"]},
        "total_count": {"type": ["integer", "null"]},
        "page_size": {"type": "integer"}
      },
      "required": ["next_page_token", "page_size"]
    }
  }
}
```

### 5.2 Database scope fragment

```json
{
  "$defs": {
    "database_scope": {
      "type": "object",
      "properties": {
        "database": {"type": ["string", "null"], "description": "Target database. Omit for default."}
      }
    }
  }
}
```

### 5.3 Object reference fragment

```json
{
  "$defs": {
    "object_ref": {
      "type": "object",
      "properties": {
        "database": {"type": "string"},
        "schema": {"type": "string"},
        "name": {"type": "string"},
        "object_id": {"type": "integer"},
        "type_desc": {"type": "string", "description": "SQL Server type_desc, e.g. USER_TABLE, VIEW, SQL_STORED_PROCEDURE"},
        "fqn": {"type": "string", "description": "[database].[schema].[name]"}
      },
      "required": ["database", "schema", "name", "object_id", "type_desc", "fqn"]
    }
  }
}
```

### 5.4 Result envelope fragment

Every tool's `structuredContent` MUST include this envelope at the top level:

```json
{
  "$defs": {
    "envelope": {
      "type": "object",
      "properties": {
        "ok": {"type": "boolean"},
        "tool": {"type": "string"},
        "duration_ms": {"type": "integer"},
        "cached_at": {"type": ["string", "null"], "format": "date-time"},
        "warnings": {"type": "array", "items": {"type": "string"}}
      },
      "required": ["ok", "tool", "duration_ms"]
    }
  }
}
```

`ok: false` implies `error` is populated per §4.1. Tool-specific output fields are siblings of the envelope fields.

---

## 6. Tool catalog

The v0 server exposes **22 tools** in seven categories. All are read-only.

Categories: **Server-info**, **Object-listing**, **Object-detail**, **Dependencies**, **Scripting**, **Performance/Health**, **Security**, **Agent/Backups**.

| #   | Tool                    | Category       | Purpose                                                                 |
| --- | ----------------------- | -------------- | ----------------------------------------------------------------------- |
| 1   | `get_server_info`       | Server-info    | Server version, edition, feature flags, configuration.                  |
| 2   | `list_databases`        | Object-listing | Databases on the instance with size, status, options.                   |
| 3   | `list_objects`          | Object-listing | Generic object listing with type filter.                                |
| 4   | `describe_table`        | Object-detail  | Rich table info: columns, indexes, FKs, constraints, stats.             |
| 5   | `describe_procedure`    | Object-detail  | Procedure parameters, return shape, body (T-SQL) or assembly ref (CLR). |
| 6   | `get_foreign_keys`      | Object-detail  | FK relationships in/out of a table.                                     |
| 7   | `get_dependencies`      | Dependencies   | Recursive dependency tree.                                              |
| 8   | `get_dependency_path`   | Dependencies   | Paths between two objects.                                              |
| 9   | `script_object`         | Scripting      | DDL CREATE/DROP for one object.                                         |
| 10  | `script_objects`        | Scripting      | DDL bundle for a set of objects, topologically ordered.                 |
| 11  | `get_top_queries`       | Performance    | Top queries by various metrics.                                         |
| 12  | `get_blocking`          | Performance    | Current blocking chains.                                                |
| 13  | `get_wait_stats`        | Performance    | Wait-stats summary.                                                     |
| 14  | `get_index_usage`       | Performance    | Index usage stats and unused indexes.                                   |
| 15  | `get_missing_indexes`   | Performance    | Missing-index suggestions.                                              |
| 16  | `analyze_db_health`     | Performance    | Composite health report.                                                |
| 17  | `list_principals`       | Security       | Flat, paginated list of logins, users, and roles.                       |
| 18  | `list_role_memberships` | Security       | Role → member edges, optionally transitive.                             |
| 19  | `find_orphaned_users`   | Security       | Database users whose SID no longer maps to a server login.              |
| 20  | `list_permissions`      | Security       | Effective and granted permissions.                                      |
| 21  | `list_jobs`             | Agent/Backups  | SQL Agent jobs, schedules, last run status.                             |
| 22  | `list_backups`          | Agent/Backups  | Backup history per database.                                            |

---

## 7. Tool specifications

For each tool: **purpose**, **input schema**, **output schema**, **behavioral notes**, **error specifics**.

---

### 7.1 `get_server_info`

**Purpose:** Return server-level information: edition, version, feature availability, and key configuration values. Called once at session start so the LLM knows what features are usable.

**Input:**

```json
{
  "type": "object",
  "properties": {
    "force_refresh": {"type": "boolean", "default": false}
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "server_name": {"type": "string"},
    "instance_name": {"type": ["string", "null"]},
    "version": {
      "type": "object",
      "properties": {
        "product_version": {"type": "string", "description": "e.g. 16.0.4115.5"},
        "major": {"type": "integer"},
        "minor": {"type": "integer"},
        "build": {"type": "integer"},
        "product_level": {"type": "string", "description": "RTM, SP1, CU, etc."},
        "edition": {"type": "string", "description": "Standard / Enterprise / Developer / Express / Azure SQL / Managed Instance"}
      },
      "required": ["product_version", "edition"]
    },
    "platform": {"type": "string", "enum": ["WINDOWS", "LINUX", "AZURE_SQL_DB", "AZURE_MI", "FABRIC", "UNKNOWN"]},
    "collation": {"type": "string"},
    "default_database": {"type": "string"},
    "is_hadr_enabled": {"type": "boolean"},
    "is_clustered": {"type": "boolean"},
    "features": {
      "type": "object",
      "description": "Boolean feature flags discovered from edition + DMVs.",
      "properties": {
        "always_on": {"type": "boolean"},
        "columnstore": {"type": "boolean"},
        "in_memory_oltp": {"type": "boolean"},
        "partitioning": {"type": "boolean"},
        "row_level_security": {"type": "boolean"},
        "dynamic_data_masking": {"type": "boolean"},
        "json_support": {"type": "boolean"},
        "graph_tables": {"type": "boolean"},
        "ledger": {"type": "boolean"},
        "agent_available": {"type": "boolean", "description": "false on Azure SQL DB; true elsewhere"},
        "xp_cmdshell_enabled": {"type": "boolean"}
      }
    },
    "current_principal": {
      "type": "object",
      "properties": {
        "name": {"type": "string"},
        "is_sysadmin": {"type": "boolean"},
        "auth_type": {"type": "string", "enum": ["WINDOWS", "SQL", "ENTRA_ID"]}
      }
    }
  },
  "required": ["server_name", "version", "platform", "features", "current_principal"]
}
```

**Behavioral notes:**

- This tool's output is naturally cacheable for the entire session. The implementation SHOULD cache and only invalidate on `force_refresh: true`.
- `agent_available: false` MUST cause `list_jobs` to return `UNSUPPORTED_FEATURE`.
- `current_principal.is_sysadmin: true` SHOULD emit a warning in `envelope.warnings[]` advising operators to use a least-privileged principal instead.

**Errors:**

- `PERMISSION_DENIED` if the principal lacks `VIEW SERVER STATE` and the server cannot populate `is_hadr_enabled` or feature detection. The tool MUST still return what it can and surface the missing capability in `warnings[]` rather than failing the whole call.

---

### 7.2 `list_databases`

**Purpose:** List databases on the instance with size, status, recovery model, compatibility level, and options.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/pagination_input"}],
  "properties": {
    "name_pattern": {"type": ["string", "null"], "description": "SQL LIKE pattern, e.g. 'app_%'."},
    "include_system": {"type": "boolean", "default": false, "description": "Include master/tempdb/model/msdb."},
    "force_refresh": {"type": "boolean", "default": false}
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}, {"$ref": "#/$defs/pagination_output"}],
  "properties": {
    "databases": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "database_id": {"type": "integer"},
          "state_desc": {"type": "string", "enum": ["ONLINE", "RESTORING", "RECOVERING", "RECOVERY_PENDING", "SUSPECT", "EMERGENCY", "OFFLINE", "COPYING", "OFFLINE_SECONDARY"]},
          "recovery_model_desc": {"type": "string", "enum": ["FULL", "BULK_LOGGED", "SIMPLE"]},
          "compatibility_level": {"type": "integer"},
          "collation_name": {"type": "string"},
          "is_read_only": {"type": "boolean"},
          "is_auto_close_on": {"type": "boolean"},
          "is_auto_shrink_on": {"type": "boolean"},
          "snapshot_isolation_state_desc": {"type": "string"},
          "is_read_committed_snapshot_on": {"type": "boolean"},
          "create_date": {"type": "string", "format": "date-time"},
          "size_mb": {"type": "number"},
          "data_size_mb": {"type": "number"},
          "log_size_mb": {"type": "number"},
          "owner": {"type": ["string", "null"]},
          "is_system_db": {"type": "boolean"}
        },
        "required": ["name", "database_id", "state_desc", "recovery_model_desc", "compatibility_level", "is_system_db"]
      }
    }
  },
  "required": ["databases"]
}
```

**Behavioral notes:**

- Size fields MAY be `null` if the principal lacks `VIEW ANY DEFINITION` on the database; the row MUST still be returned with what is visible.
- Databases in non-ONLINE states MUST still appear in the list.

**Errors:**

- `PERMISSION_DENIED` only if no databases are visible at all.

---

### 7.3 `list_objects`

**Purpose:** Generic object listing across schemas and types within a database. The primary discovery tool for tables, views, procedures, functions, triggers, indexes, synonyms, sequences, and types.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}, {"$ref": "#/$defs/pagination_input"}],
  "properties": {
    "schema": {"type": ["string", "null"], "description": "Restrict to one schema."},
    "object_type": {
      "type": ["string", "null"],
      "enum": [null, "TABLE", "VIEW", "PROCEDURE", "FUNCTION", "TRIGGER", "INDEX", "SYNONYM", "SEQUENCE", "TYPE", "XML_SCHEMA_COLLECTION", "SERVICE_QUEUE", "ANY"],
      "default": null,
      "description": "null or 'ANY' returns all types."
    },
    "name_pattern": {"type": ["string", "null"], "description": "SQL LIKE pattern."},
    "include_system": {"type": "boolean", "default": false},
    "force_refresh": {"type": "boolean", "default": false}
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}, {"$ref": "#/$defs/pagination_output"}],
  "properties": {
    "database": {"type": "string"},
    "objects": {
      "type": "array",
      "items": {
        "allOf": [{"$ref": "#/$defs/object_ref"}],
        "properties": {
          "create_date": {"type": "string", "format": "date-time"},
          "modify_date": {"type": "string", "format": "date-time"},
          "row_count": {"type": ["integer", "null"], "description": "For tables/indexes only; null otherwise."},
          "size_kb": {"type": ["number", "null"]}
        }
      }
    }
  },
  "required": ["database", "objects"]
}
```

**Behavioral notes:**

- The mapping from the input `object_type` enum to SQL Server `type_desc` values is implementation-defined but MUST be exhaustive. For example, `TABLE` MUST include `USER_TABLE` and MAY include `EXTERNAL_TABLE`.
- When `object_type == "INDEX"`, results MUST include both clustered and nonclustered, and the `name` field MUST be the index name with the `fqn` field encoded as `[database].[schema].[parent_table]:[index_name]` to disambiguate. The `type_desc` MUST be `INDEX_CLUSTERED` or `INDEX_NONCLUSTERED` etc.
- `name_pattern` is matched against the bare object name, not the FQN.

**Errors:**

- `OBJECT_NOT_FOUND` if `database` does not exist.
- `INVALID_PARAMETER` if `object_type` is not in the enum.

---

### 7.4 `describe_table`

**Purpose:** Rich, structured description of a single table: columns with types, identity/computed/default info, indexes, foreign keys (in and out), check/unique constraints, triggers attached, partitioning, compression, change tracking status, row count, size.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "schema": {"type": "string", "default": "dbo"},
    "name": {"type": "string"},
    "include_indexes": {"type": "boolean", "default": true},
    "include_foreign_keys": {"type": "boolean", "default": true},
    "include_triggers": {"type": "boolean", "default": true},
    "include_check_constraints": {"type": "boolean", "default": true},
    "include_statistics": {"type": "boolean", "default": false, "description": "Include detailed stats objects; can be expensive."},
    "force_refresh": {"type": "boolean", "default": false}
  },
  "required": ["name"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "object": {"$ref": "#/$defs/object_ref"},
    "row_count_estimate": {"type": ["integer", "null"]},
    "size_kb": {"type": ["number", "null"]},
    "is_memory_optimized": {"type": "boolean"},
    "is_temporal": {"type": "boolean"},
    "temporal_history_table": {"type": ["string", "null"]},
    "is_change_tracking_enabled": {"type": "boolean"},
    "partition_scheme": {"type": ["string", "null"]},
    "filegroup": {"type": ["string", "null"]},
    "columns": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "ordinal": {"type": "integer"},
          "data_type": {"type": "string", "description": "Full T-SQL type, e.g. 'nvarchar(50)', 'decimal(18,4)'."},
          "is_nullable": {"type": "boolean"},
          "is_identity": {"type": "boolean"},
          "is_computed": {"type": "boolean"},
          "computed_definition": {"type": ["string", "null"]},
          "is_persisted": {"type": ["boolean", "null"]},
          "default_constraint": {"type": ["string", "null"]},
          "collation": {"type": ["string", "null"]},
          "is_masked": {"type": "boolean", "description": "Dynamic data masking applied."},
          "mask_function": {"type": ["string", "null"]}
        },
        "required": ["name", "ordinal", "data_type", "is_nullable"]
      }
    },
    "primary_key": {
      "type": ["object", "null"],
      "properties": {
        "name": {"type": "string"},
        "columns": {"type": "array", "items": {"type": "string"}},
        "is_clustered": {"type": "boolean"}
      }
    },
    "indexes": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "type_desc": {"type": "string"},
          "is_unique": {"type": "boolean"},
          "is_primary_key": {"type": "boolean"},
          "is_disabled": {"type": "boolean"},
          "filter_definition": {"type": ["string", "null"]},
          "key_columns": {"type": "array", "items": {"type": "object", "properties": {"name": {"type": "string"}, "is_descending": {"type": "boolean"}}}},
          "included_columns": {"type": "array", "items": {"type": "string"}},
          "fill_factor": {"type": ["integer", "null"]}
        }
      }
    },
    "foreign_keys_outbound": {
      "type": "array",
      "description": "FKs where this table is the referencing (child) table.",
      "items": {"$ref": "#/$defs/fk_row"}
    },
    "foreign_keys_inbound": {
      "type": "array",
      "description": "FKs where this table is the referenced (parent) table.",
      "items": {"$ref": "#/$defs/fk_row"}
    },
    "check_constraints": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "definition": {"type": "string"},
          "is_disabled": {"type": "boolean"},
          "is_not_trusted": {"type": "boolean"}
        }
      }
    },
    "triggers": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "is_disabled": {"type": "boolean"},
          "is_instead_of_trigger": {"type": "boolean"},
          "events": {"type": "array", "items": {"type": "string", "enum": ["INSERT", "UPDATE", "DELETE"]}}
        }
      }
    },
    "statistics": {
      "type": "array",
      "description": "Populated only when include_statistics=true.",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "is_auto_created": {"type": "boolean"},
          "columns": {"type": "array", "items": {"type": "string"}},
          "last_updated": {"type": ["string", "null"], "format": "date-time"},
          "rows_sampled": {"type": ["integer", "null"]}
        }
      }
    }
  },
  "required": ["object", "columns"],
  "$defs": {
    "fk_row": {
      "type": "object",
      "properties": {
        "name": {"type": "string"},
        "from": {"$ref": "#/$defs/object_ref"},
        "to": {"$ref": "#/$defs/object_ref"},
        "from_columns": {"type": "array", "items": {"type": "string"}},
        "to_columns": {"type": "array", "items": {"type": "string"}},
        "delete_action": {"type": "string", "enum": ["NO_ACTION", "CASCADE", "SET_NULL", "SET_DEFAULT"]},
        "update_action": {"type": "string", "enum": ["NO_ACTION", "CASCADE", "SET_NULL", "SET_DEFAULT"]},
        "is_disabled": {"type": "boolean"},
        "is_not_trusted": {"type": "boolean"}
      }
    }
  }
}
```

**Behavioral notes:**

- The tool MUST work for `USER_TABLE`. It MAY also accept `VIEW` and `EXTERNAL_TABLE` and return a column-only subset; if so, it MUST set `warnings[]` indicating the type was not a user table.
- `row_count_estimate` is from `sys.dm_db_partition_stats` and is an estimate; the docs MUST say so.
- `data_type` MUST include length/precision/scale where applicable.

**Errors:**

- `OBJECT_NOT_FOUND` if the table does not exist.
- `AMBIGUOUS_OBJECT` if `schema` is omitted and the name resolves in multiple schemas.

---

### 7.5 `describe_procedure`

**Purpose:** Describe a stored procedure or scalar/table-valued function — either T-SQL or CLR — including parameters, return shape, and body (T-SQL) or assembly reference (CLR).

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "schema": {"type": "string", "default": "dbo"},
    "name": {"type": "string"},
    "include_body": {"type": "boolean", "default": true},
    "include_first_result_set": {"type": "boolean", "default": false, "description": "Use sys.dm_exec_describe_first_result_set; may fail on dynamic SQL."},
    "force_refresh": {"type": "boolean", "default": false}
  },
  "required": ["name"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "object": {"$ref": "#/$defs/object_ref"},
    "kind": {
      "type": "string",
      "enum": ["PROCEDURE", "CLR_PROCEDURE", "SCALAR_FUNCTION", "CLR_SCALAR_FUNCTION", "INLINE_TABLE_FUNCTION", "TABLE_FUNCTION", "CLR_TABLE_FUNCTION", "AGGREGATE_FUNCTION"],
      "description": "T-SQL variants have no prefix; CLR variants are prefixed CLR_. INLINE_TABLE_FUNCTION is T-SQL only (no CLR equivalent). AGGREGATE_FUNCTION is always CLR (no T-SQL aggregate exists)."
    },
    "is_encrypted": {"type": "boolean"},
    "is_schema_bound": {"type": "boolean"},
    "execute_as": {"type": ["string", "null"]},
    "parameters": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "ordinal": {"type": "integer"},
          "data_type": {"type": "string"},
          "is_output": {"type": "boolean"},
          "is_readonly": {"type": "boolean"},
          "has_default_value": {"type": "boolean"},
          "default_value": {"type": ["string", "null"]}
        }
      }
    },
    "return_type": {"type": ["string", "null"], "description": "For scalar functions; otherwise null."},
    "first_result_set_columns": {
      "type": ["array", "null"],
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string"},
          "ordinal": {"type": "integer"},
          "data_type": {"type": "string"},
          "is_nullable": {"type": "boolean"}
        }
      }
    },
    "body": {"type": ["string", "null"], "description": "T-SQL definition. MUST be null when is_encrypted, when include_body=false, or when the object is a CLR variant (any kind beginning with CLR_ or AGGREGATE_FUNCTION)."},
    "clr": {
      "type": ["object", "null"],
      "description": "Populated only for CLR objects (any kind beginning with CLR_ or AGGREGATE_FUNCTION). Null for T-SQL objects.",
      "properties": {
        "assembly_name": {"type": "string", "description": "Name of the assembly, e.g. 'MyLibrary'."},
        "assembly_class": {"type": "string", "description": "Fully qualified class name inside the assembly."},
        "assembly_method": {"type": "string", "description": "Method name on the class that implements the routine."},
        "assembly_id": {"type": "integer", "description": "sys.assemblies.assembly_id."},
        "execution_context_principal": {"type": ["string", "null"], "description": "EXECUTE AS principal, if declared."}
      },
      "required": ["assembly_name", "assembly_class", "assembly_method", "assembly_id"]
    }
  },
  "required": ["object", "kind", "parameters"]
}
```

**Behavioral notes:**

- **T-SQL objects** (`kind` in `PROCEDURE`, `SCALAR_FUNCTION`, `INLINE_TABLE_FUNCTION`, `TABLE_FUNCTION`):
  - `body` populated from `sys.sql_modules.definition` when available.
  - `clr` MUST be `null`.
- **CLR objects** (`kind` in `CLR_PROCEDURE`, `CLR_SCALAR_FUNCTION`, `CLR_TABLE_FUNCTION`, `AGGREGATE_FUNCTION`):
  - `body` MUST be `null` — there is no T-SQL body to return.
  - `clr` MUST be populated from `sys.assembly_modules` joined with `sys.assemblies`.
  - A warning MUST be added: `"Object is CLR; body is null by design. See 'clr' field for the assembly reference."`
  - `first_result_set_columns` MAY be `null` for CLR TVFs (`CLR_TABLE_FUNCTION`) even when `include_first_result_set=true`, because `sys.dm_exec_describe_first_result_set` cannot always resolve CLR-defined return shapes. When null in this case, a warning MUST be added.
- **Encrypted objects** (T-SQL only — CLR objects are not encryptable):
  - `body` MUST be `null` and a warning MUST be added.
  - `first_result_set_columns` will be `null` (the DMV cannot infer from an encrypted body); a warning MUST be added.
- **Non-CLR result-set inference**: `first_result_set_columns` will also be `null` when `include_first_result_set=false` OR when SQL Server cannot infer the result set shape from a T-SQL body (dynamic SQL, conditional returns); the latter MUST emit a warning.

**Errors:**

- `OBJECT_NOT_FOUND`, `AMBIGUOUS_OBJECT`, `PERMISSION_DENIED` (specifically `VIEW DEFINITION`).

---

### 7.6 `get_foreign_keys`

**Purpose:** Return inbound and outbound foreign keys for a single table. Provided as a dedicated tool because the LLM frequently asks for this in isolation and `describe_table` is heavier.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "schema": {"type": "string", "default": "dbo"},
    "name": {"type": "string"},
    "direction": {"type": "string", "enum": ["INBOUND", "OUTBOUND", "BOTH"], "default": "BOTH"}
  },
  "required": ["name"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "object": {"$ref": "#/$defs/object_ref"},
    "outbound": {"type": "array", "items": {"$ref": "#/$defs/fk_row"}},
    "inbound": {"type": "array", "items": {"$ref": "#/$defs/fk_row"}}
  },
  "required": ["object", "outbound", "inbound"]
}
```

(`fk_row` definition shared with §7.4.)

**Errors:** `OBJECT_NOT_FOUND`, `AMBIGUOUS_OBJECT`.

---

### 7.7 `get_dependencies`

**Purpose:** Return the recursive dependency tree of a database object — what it depends on (upstream), or what depends on it (downstream), or both. Walks `sys.sql_expression_dependencies`, foreign keys, and triggers, with a configurable depth limit and cycle detection.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "schema": {"type": "string", "default": "dbo"},
    "name": {"type": "string"},
    "object_type": {"type": ["string", "null"], "description": "Optional disambiguator if name resolves to multiple types."},
    "direction": {"type": "string", "enum": ["UPSTREAM", "DOWNSTREAM", "BOTH"], "default": "DOWNSTREAM"},
    "max_depth": {"type": "integer", "minimum": 1, "maximum": 20, "default": 5},
    "include_types": {"type": ["array", "null"], "items": {"type": "string"}, "default": null, "description": "Restrict walk to these type_desc values; null=all."},
    "include_columns": {"type": "boolean", "default": false, "description": "Resolve column-level dependencies via sys.dm_sql_referenced_entities."},
    "include_fks": {"type": "boolean", "default": true},
    "include_triggers": {"type": "boolean", "default": true},
    "format": {"type": "string", "enum": ["EDGES", "ADJACENCY", "ASCII", "MERMAID", "HYBRID"], "default": "HYBRID"}
  },
  "required": ["name"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "root": {"$ref": "#/$defs/object_ref"},
    "direction": {"type": "string", "enum": ["UPSTREAM", "DOWNSTREAM", "BOTH"]},
    "params_echo": {"type": "object"},
    "nodes": {
      "type": "object",
      "description": "Adjacency-map nodes, keyed by FQN.",
      "additionalProperties": {
        "type": "object",
        "properties": {
          "object_id": {"type": "integer"},
          "type_desc": {"type": "string"},
          "database": {"type": "string"},
          "schema": {"type": "string"},
          "name": {"type": "string"},
          "is_external": {"type": "boolean", "description": "true for cross-database/cross-server references."}
        }
      }
    },
    "edges_flat": {
      "type": "array",
      "description": "Flat edge list. Each edge is a parent->child relationship in the requested direction.",
      "items": {
        "type": "object",
        "properties": {
          "from": {"type": "string", "description": "FQN"},
          "to": {"type": "string", "description": "FQN"},
          "from_type": {"type": "string"},
          "to_type": {"type": "string"},
          "depth": {"type": "integer"},
          "dep_kind": {"type": "string", "enum": ["EXPRESSION", "FOREIGN_KEY", "TRIGGER", "SCHEMA_BOUND", "COMPUTED_COLUMN", "DEFAULT_CONSTRAINT", "CHECK_CONSTRAINT"]},
          "is_schema_bound": {"type": "boolean"},
          "is_caller_dependent": {"type": "boolean"},
          "is_ambiguous": {"type": "boolean"},
          "referenced_database": {"type": ["string", "null"]},
          "referenced_server": {"type": ["string", "null"]},
          "referenced_column": {"type": ["string", "null"], "description": "Populated only when include_columns=true."}
        },
        "required": ["from", "to", "depth", "dep_kind"]
      }
    },
    "edges_adjacency": {
      "type": "object",
      "description": "Adjacency map: FQN -> list of child FQNs.",
      "additionalProperties": {"type": "array", "items": {"type": "string"}}
    },
    "ascii_tree": {"type": ["string", "null"], "description": "Populated when format is ASCII or HYBRID."},
    "mermaid": {"type": ["string", "null"], "description": "Populated when format is MERMAID or HYBRID."},
    "summary": {
      "type": "object",
      "properties": {
        "total_nodes": {"type": "integer"},
        "total_edges": {"type": "integer"},
        "max_depth_reached": {"type": "integer"},
        "schema_bound_edges": {"type": "integer"},
        "by_type": {"type": "object", "additionalProperties": {"type": "integer"}},
        "cycles_detected": {"type": "integer"},
        "cycle_paths": {"type": "array", "items": {"type": "array", "items": {"type": "string"}}},
        "cross_database_refs": {"type": "integer"},
        "unresolved_refs": {"type": "integer"},
        "truncated": {"type": "boolean"}
      },
      "required": ["total_nodes", "total_edges", "max_depth_reached", "cycles_detected", "truncated"]
    }
  },
  "required": ["root", "direction", "nodes", "edges_flat", "summary"]
}
```

**Behavioral notes:**

- Cycle detection MUST visit each node (keyed on `object_id` + database) at most once per path; on revisit, the cycle MUST be recorded in `summary.cycle_paths` and walking down that branch MUST stop.
- When `direction == "BOTH"`, the response combines both walks; the `depth` field encodes signed depth (negative for upstream, positive for downstream) and the root has `depth: 0`.
- If `format == "EDGES"`, the response MAY omit `nodes`, `edges_adjacency`, `ascii_tree`, and `mermaid` to save tokens, but `summary` MUST always be present.
- For `BOTH` requests, `ascii_tree` MUST render upstream and downstream as separate sections.
- This tool SHOULD emit progress notifications per §2.10 when `max_depth >= 5` or `direction = "BOTH"`.

**Errors:**

- `OBJECT_NOT_FOUND`, `AMBIGUOUS_OBJECT`.
- `RESULT_TOO_LARGE` if the walk would exceed an internal node-count cap (default 5000); when this happens, the partial result MUST still be returned with `summary.truncated=true`.

---

### 7.8 `get_dependency_path`

**Purpose:** Return the path(s) connecting two specific objects in the dependency graph. Mirrors DataHub's `get_lineage_paths_between`. Used to answer "how is A connected to B?".

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "from": {"type": "string", "description": "FQN or schema.name."},
    "to": {"type": "string", "description": "FQN or schema.name."},
    "max_depth": {"type": "integer", "minimum": 1, "maximum": 20, "default": 10},
    "max_paths": {"type": "integer", "minimum": 1, "maximum": 50, "default": 10},
    "include_columns": {"type": "boolean", "default": false}
  },
  "required": ["from", "to"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "from": {"$ref": "#/$defs/object_ref"},
    "to": {"$ref": "#/$defs/object_ref"},
    "paths": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "length": {"type": "integer"},
          "nodes": {"type": "array", "items": {"type": "string", "description": "FQN"}},
          "edges": {"type": "array", "items": {"type": "object", "properties": {"dep_kind": {"type": "string"}, "is_schema_bound": {"type": "boolean"}}}}
        }
      }
    },
    "no_path": {"type": "boolean"},
    "truncated": {"type": "boolean", "description": "True when more than max_paths exist."}
  },
  "required": ["from", "to", "paths", "no_path", "truncated"]
}
```

**Behavioral notes:**

- The shortest path MUST come first; ties are broken by lexicographic ordering of node FQNs.
- If no path exists in either direction, `no_path: true` and `paths: []`.

**Errors:** `OBJECT_NOT_FOUND` (for either endpoint).

---

### 7.9 `script_object`

**Purpose:** Generate the DDL script for a single object: `CREATE`, `DROP`, or both. Supports tables, views, procedures, functions, triggers, indexes, synonyms, sequences, types, schemas, logins, roles.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "schema": {"type": "string", "default": "dbo"},
    "name": {"type": "string"},
    "object_type": {"type": ["string", "null"], "description": "Disambiguator when name resolves to multiple types."},
    "operations": {
      "type": "array",
      "items": {"type": "string", "enum": ["CREATE", "DROP", "DROP_AND_CREATE"]},
      "default": ["CREATE"],
      "description": "What to script. DROP_AND_CREATE emits a DROP IF EXISTS followed by CREATE."
    },
    "options": {
      "type": "object",
      "properties": {
        "include_if_not_exists": {"type": "boolean", "default": true},
        "include_drop_if_exists": {"type": "boolean", "default": true},
        "include_indexes": {"type": "boolean", "default": true, "description": "For tables."},
        "include_foreign_keys": {"type": "boolean", "default": true, "description": "For tables."},
        "include_check_constraints": {"type": "boolean", "default": true},
        "include_triggers": {"type": "boolean", "default": false, "description": "For tables; triggers as separate scripts in the same bundle."},
        "include_permissions": {"type": "boolean", "default": false},
        "include_extended_properties": {"type": "boolean", "default": false},
        "include_collation": {"type": "boolean", "default": false},
        "include_object_schema_qualifier": {"type": "boolean", "default": true},
        "ansi_padding": {"type": ["boolean", "null"], "default": null},
        "encoding": {"type": "string", "enum": ["UTF8", "UTF16"], "default": "UTF8"}
      }
    }
  },
  "required": ["name"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "object": {"$ref": "#/$defs/object_ref"},
    "operations_emitted": {"type": "array", "items": {"type": "string"}},
    "scripts": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "operation": {"type": "string", "enum": ["CREATE", "DROP", "DROP_AND_CREATE"]},
          "language": {"type": "string", "enum": ["TSQL"]},
          "body": {"type": "string", "description": "The DDL text."},
          "size_bytes": {"type": "integer"},
          "warnings": {"type": "array", "items": {"type": "string"}}
        },
        "required": ["operation", "language", "body"]
      }
    }
  },
  "required": ["object", "scripts"]
}
```

**Behavioral notes:**

- The DDL body MUST be plain T-SQL with `GO` separators where statement separation is required (e.g., between `CREATE PROCEDURE` batches and subsequent permission grants).
- For tables, when `include_indexes=true`, indexes MUST be emitted as `CREATE INDEX` statements *after* the `CREATE TABLE` rather than inline, except for the primary key and unique constraints, which MUST be inline.
- `include_permissions=true` MUST emit `GRANT`/`DENY`/`REVOKE` statements that reproduce current permissions on the object.
- The script MUST NOT include the password of any login (passwords are not retrievable from the catalog); `CREATE LOGIN` scripts MUST emit `WITH PASSWORD = N'<REPLACE_ME>'` and add a warning.
- The script MUST be idempotent when `include_drop_if_exists=true` and `operations` includes `DROP_AND_CREATE`.

**Errors:**

- `OBJECT_NOT_FOUND`, `AMBIGUOUS_OBJECT`.
- `UNSUPPORTED_FEATURE` if the object type is not scriptable (e.g., system objects, in-memory OLTP under specific conditions).

---

### 7.10 `script_objects`

**Purpose:** Generate a single, topologically ordered DDL bundle for a set of objects. Used for "script this whole subsystem" or "give me everything I need to recreate this view and its dependencies." Internally orchestrates dependency resolution + scripting; the LLM does not need to compose multiple `script_object` calls.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}],
  "properties": {
    "objects": {
      "type": "array",
      "minItems": 1,
      "maxItems": 500,
      "items": {
        "type": "object",
        "properties": {
          "schema": {"type": "string", "default": "dbo"},
          "name": {"type": "string"},
          "object_type": {"type": ["string", "null"]}
        },
        "required": ["name"]
      }
    },
    "include_dependencies": {
      "type": "string",
      "enum": ["NONE", "DIRECT", "TRANSITIVE"],
      "default": "TRANSITIVE",
      "description": "Whether to expand the input set with dependencies. NONE=script only the input list; DIRECT=add one-hop upstream; TRANSITIVE=add full upstream closure."
    },
    "max_dependency_depth": {"type": "integer", "minimum": 1, "maximum": 20, "default": 10, "description": "Caps transitive expansion."},
    "operations": {
      "type": "array",
      "items": {"type": "string", "enum": ["CREATE", "DROP", "DROP_AND_CREATE"]},
      "default": ["CREATE"]
    },
    "options": {
      "type": "object",
      "description": "Same options object as script_object §7.9, applied to every script in the bundle."
    },
    "delivery": {
      "type": "string",
      "enum": ["SINGLE_SCRIPT", "PER_OBJECT"],
      "default": "SINGLE_SCRIPT",
      "description": "SINGLE_SCRIPT concatenates everything in topological order. PER_OBJECT returns an array of scripts."
    }
  },
  "required": ["objects"],
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "input_count": {"type": "integer"},
    "expanded_count": {"type": "integer", "description": "Total objects scripted after dependency expansion."},
    "topological_order": {
      "type": "array",
      "items": {"type": "string", "description": "FQN in CREATE order. Reverse this for DROP order."}
    },
    "delivery": {"type": "string", "enum": ["SINGLE_SCRIPT", "PER_OBJECT"]},
    "single_script": {
      "type": ["string", "null"],
      "description": "Populated when delivery=SINGLE_SCRIPT."
    },
    "scripts": {
      "type": ["array", "null"],
      "description": "Populated when delivery=PER_OBJECT.",
      "items": {
        "type": "object",
        "properties": {
          "object": {"$ref": "#/$defs/object_ref"},
          "operation": {"type": "string"},
          "body": {"type": "string"},
          "depends_on": {"type": "array", "items": {"type": "string"}, "description": "FQNs of objects that must be scripted before this one."}
        }
      }
    },
    "cycles_detected": {"type": "integer"},
    "cycle_paths": {"type": "array", "items": {"type": "array", "items": {"type": "string"}}},
    "unresolved_refs": {"type": "array", "items": {"type": "string"}, "description": "Referenced objects that could not be located."},
    "skipped": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "object": {"$ref": "#/$defs/object_ref"},
          "reason": {"type": "string"}
        }
      }
    }
  },
  "required": ["input_count", "expanded_count", "topological_order", "delivery"]
}
```

**Behavioral notes:**

- The `topological_order` is the canonical CREATE order. For `DROP`/`DROP_AND_CREATE` operations, the server MUST emit DROPs in the *reverse* of `topological_order`, then CREATEs in `topological_order` (in that single concatenated sequence when `delivery=SINGLE_SCRIPT`).
- If a cycle is detected (e.g., mutually recursive procedures), the server MUST break the cycle by emitting the relevant CREATEs as `CREATE OR ALTER` style stubs first, then full bodies, and document this in `cycle_paths`. This MUST add a warning.
- Cross-database references in the input set MUST cause the offending objects to appear in `skipped[]` with reason `cross_database_dependency_not_in_scope`, unless the caller has supplied them as part of the input set.
- `unresolved_refs` MUST list any name that appears in dependency expansion but cannot be resolved (typo, dropped object, dynamic SQL).
- The bundle MUST start with a `USE [database]` statement if all objects belong to the same database.
- Deduplication: objects requested multiple times in `objects[]` (or appearing both in the input and via dependency expansion) MUST be scripted exactly once.
- This tool SHOULD emit progress notifications per §2.10 (per-object increments).

**Errors:**

- `OBJECT_NOT_FOUND` only if every object in the input list is missing; otherwise missing objects go to `skipped[]`.
- `RESULT_TOO_LARGE` if `expanded_count` would exceed an internal cap (default 1000).

---

### 7.11 `get_top_queries`

**Purpose:** Top queries by CPU, duration, logical reads, physical reads, or execution count, drawn from `sys.dm_exec_query_stats` and related DMVs.

**Input:**

```json
{
  "type": "object",
  "properties": {
    "by": {"type": "string", "enum": ["TOTAL_CPU", "AVG_CPU", "TOTAL_DURATION", "AVG_DURATION", "TOTAL_LOGICAL_READS", "TOTAL_PHYSICAL_READS", "EXECUTION_COUNT"], "default": "TOTAL_CPU"},
    "top": {"type": "integer", "minimum": 1, "maximum": 100, "default": 20},
    "database": {"type": ["string", "null"], "description": "Restrict to queries against this database."},
    "min_executions": {"type": "integer", "default": 1},
    "include_query_text": {"type": "boolean", "default": true},
    "include_query_plan": {"type": "boolean", "default": false},
    "row_limit": {"type": "integer", "default": 100, "maximum": 1000}
  },
  "additionalProperties": false
}
```

**Output:** structured `{queries: [...]}` with per-query: `query_hash`, `plan_hash`, `database`, `execution_count`, `total_*` and `avg_*` metrics, optional `query_text` and `query_plan_xml`. Tabular form (CSV) accompanies in `content`.

**Errors:** `PERMISSION_DENIED` if `VIEW SERVER STATE` is missing.

---

### 7.12 `get_blocking`

**Purpose:** Current blocking chains from `sys.dm_exec_requests` + `sys.dm_os_waiting_tasks`.

**Input:** `{database?: string, include_query_text?: boolean (default true)}`.

**Output:** array of blocking-chain trees with head blocker, blocked sessions, wait types, durations, query text. The tree structure MUST surface the head blocker at the root and use the same adjacency-map shape as §7.7 for consistency.

**Errors:** `PERMISSION_DENIED` (`VIEW SERVER STATE`).

---

### 7.13 `get_wait_stats`

**Purpose:** Aggregated wait-stats summary from `sys.dm_os_wait_stats`, with an option to exclude benign waits (the canonical Paul Randal exclusion list).

**Input:**

```json
{
  "type": "object",
  "properties": {
    "exclude_benign": {"type": "boolean", "default": true},
    "top": {"type": "integer", "minimum": 1, "maximum": 100, "default": 20},
    "since_last_clear": {"type": "boolean", "default": true, "description": "If false, returns since SQL Server start."}
  },
  "additionalProperties": false
}
```

**Output:** array with `wait_type`, `waiting_tasks_count`, `wait_time_ms`, `max_wait_time_ms`, `signal_wait_time_ms`, `pct_of_total`. Tabular accompaniment in CSV.

---

### 7.14 `get_index_usage`

**Purpose:** Index usage and unused-index detection from `sys.dm_db_index_usage_stats`.

**Input:** `{database, schema?, table?, only_unused?: boolean (default false), since_uptime_only?: boolean (default true)}`.

**Output:** rows with `database`, `schema`, `table`, `index_name`, `index_type`, `user_seeks`, `user_scans`, `user_lookups`, `user_updates`, `last_user_seek`, `last_user_scan`, `last_user_update`, `is_unique`, `is_primary_key`. When `only_unused=true`, MUST filter to indexes with zero reads and non-zero writes.

---

### 7.15 `get_missing_indexes`

**Purpose:** Missing-index suggestions from `sys.dm_db_missing_index_*` DMVs.

**Input:** `{database?, min_impact?: number (default 1000), top?: integer (default 50)}`.

**Output:** rows with `database`, `schema`, `table`, `equality_columns`, `inequality_columns`, `included_columns`, `unique_compiles`, `user_seeks`, `user_scans`, `last_user_seek`, `avg_total_user_cost`, `avg_user_impact`, `improvement_measure` (computed), `suggested_create_statement` (textual `CREATE INDEX` template).

**Behavioral notes:** the output explicitly DOES NOT execute the suggestion; the `suggested_create_statement` is text only.

---

### 7.16 `analyze_db_health`

**Purpose:** Composite health report for a single database. Internally orchestrates blocking, top waits, missing/unused indexes, fragmentation, tempdb pressure, and configuration warnings into one structured report. The "agentic DBA" entry point.

**Input:**

```json
{
  "type": "object",
  "properties": {
    "database": {"type": ["string", "null"], "description": "null=scope to instance-level checks only."},
    "checks": {
      "type": "array",
      "items": {"type": "string", "enum": ["BLOCKING", "WAITS", "INDEX_HEALTH", "FRAGMENTATION", "TEMPDB", "CONFIGURATION", "BACKUPS", "FILE_GROWTH", "AG_HEALTH"]},
      "default": ["BLOCKING", "WAITS", "INDEX_HEALTH", "FRAGMENTATION", "TEMPDB", "CONFIGURATION", "BACKUPS"]
    },
    "depth": {"type": "string", "enum": ["QUICK", "STANDARD", "DEEP"], "default": "STANDARD"}
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "database": {"type": ["string", "null"]},
    "overall_status": {"type": "string", "enum": ["OK", "WARN", "CRITICAL"]},
    "score": {"type": "integer", "minimum": 0, "maximum": 100},
    "findings": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "check": {"type": "string"},
          "severity": {"type": "string", "enum": ["INFO", "WARN", "CRITICAL"]},
          "title": {"type": "string"},
          "details": {"type": "string"},
          "evidence": {"type": "object", "description": "Structured payload specific to the check."},
          "remediation_hint": {"type": "string"}
        },
        "required": ["check", "severity", "title", "details"]
      }
    },
    "checks_run": {"type": "array", "items": {"type": "string"}},
    "checks_skipped": {"type": "array", "items": {"type": "object", "properties": {"check": {"type": "string"}, "reason": {"type": "string"}}}}
  },
  "required": ["overall_status", "findings", "checks_run"]
}
```

**Behavioral notes:**

- `overall_status` is derived: any `CRITICAL` finding → `CRITICAL`; else any `WARN` → `WARN`; else `OK`.
- Each check MUST be independently skippable: a `PERMISSION_DENIED` from one DMV MUST move that check into `checks_skipped[]` and not abort the others.
- `depth=QUICK` MUST complete within 5 seconds against a healthy server.
- `remediation_hint` MUST be advisory only and SHOULD NOT include directly executable SQL. (There is no `execute_sql` in this server; nevertheless, advisory hints are kept declarative to discourage downstream automation from running them blindly.)
- This tool SHOULD emit progress notifications per §2.10 (per-check increments).

**Errors:** rare — most failures degrade to skipped checks. `INTERNAL_ERROR` only on totally unrecoverable conditions.

---

### 7.17 `list_principals`

**Purpose:** Flat, paginated list of security principals — server logins, database users, server roles, and database roles. Each item carries a `principal_kind` discriminator so the caller can filter downstream. Backed by `sys.server_principals` and `sys.database_principals`.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}, {"$ref": "#/$defs/pagination_input"}],
  "properties": {
    "scope": {
      "type": "string",
      "enum": ["SERVER", "DATABASE", "BOTH"],
      "default": "BOTH",
      "description": "SERVER queries sys.server_principals only. DATABASE queries sys.database_principals for the target database. BOTH does both."
    },
    "principal_type": {
      "type": "string",
      "enum": ["LOGIN", "USER", "SERVER_ROLE", "DATABASE_ROLE", "ANY"],
      "default": "ANY",
      "description": "Filter to one principal kind. ANY returns all kinds allowed by scope."
    },
    "name_pattern": {
      "type": ["string", "null"],
      "description": "SQL LIKE pattern matched against the principal name."
    },
    "include_system": {
      "type": "boolean",
      "default": false,
      "description": "Include built-in principals (sa, ##MS_*##, NT SERVICE\\*, fixed server/database roles). Default false to keep the LLM focused on user-authored principals."
    },
    "force_refresh": {"type": "boolean", "default": false}
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}, {"$ref": "#/$defs/pagination_output"}],
  "properties": {
    "principals": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "principal_kind": {"type": "string", "enum": ["SERVER_LOGIN", "DATABASE_USER", "SERVER_ROLE", "DATABASE_ROLE"]},
          "name": {"type": "string"},
          "type_desc": {"type": "string", "description": "SQL Server-native type_desc, e.g. SQL_LOGIN, WINDOWS_LOGIN, EXTERNAL_LOGIN (Entra ID), SQL_USER, WINDOWS_USER, EXTERNAL_USER, SERVER_ROLE, DATABASE_ROLE, CERTIFICATE_MAPPED_LOGIN, ASYMMETRIC_KEY_MAPPED_LOGIN."},
          "database": {"type": ["string", "null"], "description": "Populated for DATABASE_USER and DATABASE_ROLE; null for server-scoped principals."},
          "principal_id": {"type": "integer"},
          "sid": {"type": ["string", "null"], "description": "Hex-encoded SID for logins and users; null for roles."},
          "auth_type": {"type": ["string", "null"], "enum": [null, "WINDOWS", "SQL", "ENTRA_ID", "CERTIFICATE", "ASYMMETRIC_KEY"], "description": "For logins only."},
          "is_disabled": {"type": ["boolean", "null"], "description": "For server logins only."},
          "default_database": {"type": ["string", "null"], "description": "For server logins only."},
          "default_schema": {"type": ["string", "null"], "description": "For database users only."},
          "is_fixed_role": {"type": ["boolean", "null"], "description": "For roles only."},
          "owning_principal": {"type": ["string", "null"], "description": "For roles: the principal that owns the role."},
          "create_date": {"type": "string", "format": "date-time"},
          "modify_date": {"type": "string", "format": "date-time"}
        },
        "required": ["principal_kind", "name", "type_desc", "principal_id", "create_date"]
      }
    }
  },
  "required": ["principals"]
}
```

**Behavioral notes:**

- When `scope="DATABASE"` and no `database` parameter is supplied, the default database rule from §2.4 applies. When `scope="BOTH"` and `database` is supplied, both `sys.server_principals` and the specified database's `sys.database_principals` are queried.
- `principal_kind` is derived: `SERVER_ROLE` and `DATABASE_ROLE` are set for principals whose `type_desc` ends in `_ROLE`; otherwise `SERVER_LOGIN` (from `sys.server_principals`) or `DATABASE_USER` (from `sys.database_principals`).
- Pagination is over the merged, sorted result. Sort order MUST be stable: `(scope_kind ASC, database ASC NULLS FIRST, principal_kind ASC, name ASC)` so that pages don't shift between calls.
- Server logins that have no corresponding database user (or vice versa) appear on their own; this tool does NOT join the two sides. Use `list_role_memberships` or `find_orphaned_users` for cross-side queries.

**Errors:**

- `PERMISSION_DENIED` (typically `VIEW ANY DEFINITION` for cross-database visibility; `VIEW SERVER STATE` for server-scoped queries).
- `OBJECT_NOT_FOUND` if `scope="DATABASE"` and the target database does not exist.

---

### 7.18 `list_role_memberships`

**Purpose:** Role → member edges at server and/or database scope. Optionally expands transitively so nested role chains (role A contains role B contains user U) yield the effective flat set for security auditing.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}, {"$ref": "#/$defs/pagination_input"}],
  "properties": {
    "scope": {
      "type": "string",
      "enum": ["SERVER", "DATABASE", "BOTH"],
      "default": "BOTH"
    },
    "role": {
      "type": ["string", "null"],
      "description": "Filter to memberships of this role name. Case-insensitive. Combines with member if both are supplied."
    },
    "member": {
      "type": ["string", "null"],
      "description": "Filter to memberships held by this principal. Case-insensitive."
    },
    "include_transitive": {
      "type": "boolean",
      "default": false,
      "description": "When true, expand nested role memberships. Direct edges are marked is_inherited=false; edges reached through intermediate roles are is_inherited=true with inherited_via set to the intermediate role chain."
    },
    "include_system": {
      "type": "boolean",
      "default": false,
      "description": "Include memberships involving built-in fixed roles (sysadmin, db_owner, etc.). Default false."
    }
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}, {"$ref": "#/$defs/pagination_output"}],
  "properties": {
    "memberships": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "scope": {"type": "string", "enum": ["SERVER", "DATABASE"]},
          "database": {"type": ["string", "null"], "description": "Populated when scope=DATABASE."},
          "role": {"type": "string"},
          "role_type_desc": {"type": "string", "enum": ["SERVER_ROLE", "DATABASE_ROLE"]},
          "member": {"type": "string"},
          "member_type_desc": {"type": "string", "description": "SQL Server type_desc of the member: SQL_LOGIN, WINDOWS_LOGIN, EXTERNAL_LOGIN, SQL_USER, WINDOWS_USER, EXTERNAL_USER, SERVER_ROLE, DATABASE_ROLE, etc."},
          "is_inherited": {"type": "boolean", "description": "True when this edge was derived via transitive expansion. False for direct catalog edges."},
          "inherited_via": {
            "type": ["array", "null"],
            "items": {"type": "string"},
            "description": "When is_inherited=true, the chain of intermediate role names from the direct role to this member. Null when is_inherited=false."
          }
        },
        "required": ["scope", "role", "role_type_desc", "member", "member_type_desc", "is_inherited"]
      }
    }
  },
  "required": ["memberships"]
}
```

**Behavioral notes:**

- Direct edges come from `sys.server_role_members` (server scope) and `sys.database_role_members` (database scope). Both catalog views expose `role_principal_id` and `member_principal_id`; the tool joins to `sys.server_principals` / `sys.database_principals` for names and type descriptions.
- **Transitive expansion**: when `include_transitive=true`, the server performs a breadth-first walk from each role to its members, then recurses through any role-member children. Every derived edge is emitted with `is_inherited=true` and `inherited_via` populated with the intermediate role names. Depth is capped at 10 to prevent runaway on pathological configurations; a warning MUST be added when the cap is reached.
- **Cycle handling**: SQL Server permits nested roles that could theoretically cycle. The transitive walker MUST maintain a visited-set per starting role and stop on revisit, adding the cycle to `envelope.warnings[]`.
- Pagination sort order: `(scope ASC, database ASC NULLS FIRST, role ASC, member ASC, is_inherited ASC)`.
- When `role` and `member` are both null and `include_transitive=false`, the tool returns the raw catalog data. This is the cheapest and most common form.

**Errors:**

- `PERMISSION_DENIED` (`VIEW ANY DEFINITION`).
- `OBJECT_NOT_FOUND` if `scope="DATABASE"` and the target database does not exist. A `role` or `member` name that doesn't exist returns an empty result set (not an error) — the caller may want to know that "sysadmin has no members" without a lookup failure.

---

### 7.19 `find_orphaned_users`

**Purpose:** Diagnostic tool that returns database users whose SID no longer maps to any server login. These are the classic "orphaned users" that block database restores from other instances and prevent principals from connecting. Small result set by design; no pagination.

**Input:**

```json
{
  "type": "object",
  "properties": {
    "database": {
      "type": ["string", "null"],
      "description": "Target database. Null checks all accessible non-system databases."
    },
    "include_system_databases": {
      "type": "boolean",
      "default": false,
      "description": "Include master/tempdb/model/msdb in the scan when database is null."
    }
  },
  "additionalProperties": false
}
```

**Output:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/envelope"}],
  "properties": {
    "orphaned_users": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "database": {"type": "string"},
          "name": {"type": "string"},
          "sid": {"type": "string", "description": "Hex-encoded SID that did not match any sys.server_principals row."},
          "type_desc": {"type": "string"},
          "create_date": {"type": "string", "format": "date-time"},
          "default_schema": {"type": ["string", "null"]}
        },
        "required": ["database", "name", "sid", "type_desc", "create_date"]
      }
    },
    "databases_checked": {
      "type": "array",
      "items": {"type": "string"},
      "description": "Databases that were scanned successfully."
    },
    "databases_skipped": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "database": {"type": "string"},
          "reason": {"type": "string", "enum": ["PERMISSION_DENIED", "OFFLINE", "RESTORING", "OTHER"]},
          "message": {"type": "string"}
        }
      }
    }
  },
  "required": ["orphaned_users", "databases_checked", "databases_skipped"]
}
```

**Behavioral notes:**

- The detection query is a left-anti-join between `sys.database_principals` and `sys.server_principals` on `sid`, filtered to user-authored `type_desc` values (SQL_USER, WINDOWS_USER, EXTERNAL_USER); role and system principals are excluded because they legitimately have no server login.
- Windows users mapped from AD groups also appear as orphaned when the group has been renamed or deleted; the tool does NOT distinguish these from truly orphaned users, but the `type_desc` (`WINDOWS_USER`) is a strong hint.
- A database that cannot be scanned (permissions denied, database offline, in recovery) MUST be listed in `databases_skipped` rather than causing the whole call to fail. This mirrors the behavior of `analyze_db_health`.
- Cross-database iteration is done in a single connection using `USE [database]` statements confined to the tool call, per §2.4.
- Result count is bounded by SQL Server itself: even a very messy production instance is unlikely to have more than a few hundred orphans. No pagination is provided; if the array is somehow enormous, add a `truncated` field in a future revision.

**Errors:**

- `PERMISSION_DENIED` only if no databases could be scanned at all. Otherwise partial results with `databases_skipped[]`.
- `OBJECT_NOT_FOUND` if `database` is specified and does not exist.

---

### 7.20 `list_permissions`

**Purpose:** List effective and granted permissions, scoped by principal or by securable.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/database_scope"}, {"$ref": "#/$defs/pagination_input"}],
  "properties": {
    "principal_name": {"type": ["string", "null"], "description": "Filter to a single principal."},
    "securable_type": {"type": ["string", "null"], "enum": [null, "SERVER", "DATABASE", "SCHEMA", "OBJECT"], "default": null},
    "securable_name": {"type": ["string", "null"], "description": "FQN of the securable."},
    "include_inherited": {"type": "boolean", "default": true, "description": "Include role-inherited permissions for principal_name."}
  },
  "additionalProperties": false
}
```

**Output:** flat array of `{principal, principal_type, permission_name, state (GRANT|DENY|REVOKE), securable, securable_type, grantor, is_inherited, inherited_via_role}` rows. CSV mirror in `content`.

**Errors:** `PERMISSION_DENIED`.

---

### 7.21 `list_jobs`

**Purpose:** SQL Agent jobs, schedules, and recent run history.

**Input:**

```json
{
  "type": "object",
  "allOf": [{"$ref": "#/$defs/pagination_input"}],
  "properties": {
    "name_pattern": {"type": ["string", "null"]},
    "enabled_only": {"type": "boolean", "default": false},
    "include_history": {"type": "boolean", "default": true},
    "history_days": {"type": "integer", "minimum": 1, "maximum": 90, "default": 7},
    "include_steps": {"type": "boolean", "default": false},
    "include_schedules": {"type": "boolean", "default": true},
    "status_filter": {"type": ["string", "null"], "enum": [null, "FAILED_RECENT", "RUNNING", "SUCCEEDED_RECENT"], "default": null}
  },
  "additionalProperties": false
}
```

**Output:** structured `{jobs: [{job_id (uuid), name, enabled, owner, category, description, last_run_date, last_run_outcome (SUCCEEDED|FAILED|RETRY|CANCELED|UNKNOWN), last_run_duration_seconds, next_run_date, schedules: [...], steps: [...], history: [{run_date, outcome, duration_seconds, message}]}]}`.

**Behavioral notes:**

- MUST return `UNSUPPORTED_FEATURE` when running against Azure SQL Database (Agent unavailable).
- The tool MUST NOT expose tools to start/stop/disable jobs in v0.

**Errors:** `UNSUPPORTED_FEATURE` (Azure SQL DB), `PERMISSION_DENIED` (`SQLAgentReader` role membership absent).

---

### 7.22 `list_backups`

**Purpose:** Backup history per database from `msdb.dbo.backupset` and related tables.

**Input:**

```json
{
  "type": "object",
  "properties": {
    "database": {"type": ["string", "null"], "description": "null=all databases."},
    "since": {"type": ["string", "null"], "format": "date-time"},
    "backup_type": {"type": ["string", "null"], "enum": [null, "FULL", "DIFFERENTIAL", "LOG", "FILEGROUP", "ANY"], "default": "ANY"},
    "top": {"type": "integer", "minimum": 1, "maximum": 1000, "default": 100},
    "include_files": {"type": "boolean", "default": false}
  },
  "additionalProperties": false
}
```

**Output:** rows with `database`, `backup_type`, `backup_start_date`, `backup_finish_date`, `duration_seconds`, `backup_size_mb`, `compressed_size_mb`, `is_copy_only`, `recovery_model`, `media_set_id`, `physical_device_name[]` (when `include_files=true`), `first_lsn`, `last_lsn`, `checkpoint_lsn`. Plus a top-level `summary` per database with `last_full`, `last_diff`, `last_log`, `rpo_minutes` (computed against now).

**Errors:** `UNSUPPORTED_FEATURE` against Azure SQL DB (use the platform's automated backup catalog instead, which is out of scope here).

---

## 8. Cross-cutting requirements

### 8.1 Tool description token budget

Each tool's `description` field (visible in `tools/list`) MUST be ≤ 1000 characters. The combined `tools/list` payload MUST be ≤ 30 KB. Implementations approaching the upper bound SHOULD move detailed parameter docs into the input schema's `description` per-property fields rather than the top-level tool description.

### 8.2 Logging and observability

- The server SHOULD emit structured logs (one JSON object per line) for every tool invocation, capturing: `tool`, `duration_ms`, `ok`, `error.code` (if any), `database`, `principal`, and a coarse `parameters_summary` (NOT the full parameters — connection strings, parameter values, and identifier patterns that could leak sensitive content MUST NOT be logged).
- The server MUST NOT log full DDL bodies returned by scripting tools, or any column-level data.
- The server MAY expose OpenTelemetry traces. If it does, span names MUST match tool names.
- For stdio transport, log destination MUST be stderr (stdout is reserved for the JSON-RPC protocol channel).

### 8.3 Configuration surface

The server MUST accept the following configuration (mechanism implementation-defined, but all MUST be supported):

| Key                                   | Required    | Purpose                                                                        |
| ------------------------------------- | ----------- | ------------------------------------------------------------------------------ |
| `connection.server`                   | yes         | Hostname or DNS of the SQL Server instance.                                    |
| `connection.port`                     | no          | Default 1433.                                                                  |
| `connection.auth_method`              | yes         | `WINDOWS` / `SQL` / `ENTRA_ID` / `MANAGED_IDENTITY`.                           |
| `connection.username`                 | conditional | Required for `SQL`.                                                            |
| `connection.password`                 | conditional | Required for `SQL`; MUST be sourced from a secret store, not plaintext config. |
| `connection.database`                 | no          | Default database for tools that omit `database`.                               |
| `connection.encrypt`                  | no          | Default `true`.                                                                |
| `connection.trust_server_certificate` | no          | Default `false`. MUST be settable per the user's environment.                  |
| `connection.application_intent`       | no          | `READWRITE` (default) or `READONLY` (routes to AlwaysOn read replica).         |
| `connection.application_name`         | no          | Default `mssql-admin-mcp/<version>`.                                           |
| `limits.default_row_limit`            | no          | Default 500.                                                                   |
| `limits.max_row_limit`                | no          | Default 10000.                                                                 |
| `limits.default_timeout_seconds`      | no          | Default 30.                                                                    |
| `limits.max_concurrent_tool_calls`    | no          | Default 4.                                                                     |
| `cache.metadata_ttl_seconds`          | no          | 0 disables; default 300.                                                       |

The server MUST refuse to start if `connection.auth_method` is `SQL` and `connection.password` is not provided via a secret-store reference.

### 8.4 Versioning

- The MCP server version is independent of the spec version.
- The server MUST advertise the spec version it implements via a `_meta.spec_version` field on every tool result envelope (suggested: a string like `"v0.4"`).
- Breaking changes to any tool's input or output schema MUST bump the spec major or minor version per SemVer-for-APIs conventions.

### 8.5 Localization

- All human-readable strings in tool outputs (warnings, error messages, finding titles) MUST be English in v0.
- Tool inputs MUST accept Unicode object names (databases, schemas, columns) per SQL Server identifier rules. Identifier comparisons MUST honor target collation case-sensitivity.

### 8.6 Concurrency

- The server MAY serialize tool calls per session or process them concurrently up to a configured pool size (`limits.max_concurrent_tool_calls`, default 4).
- The server MUST gracefully reject calls beyond its capacity with `INTERNAL_ERROR` and `retryable: true`. Clients are expected to back off.

### 8.7 Untrusted-content hardening

Tool *outputs* may contain text from user data (table comments, `extended_properties`, procedure bodies, error messages, job names). This text MAY contain prompt-injection payloads. The server MUST:

- NOT interpret tool output text as instructions itself (the server is not an LLM).
- Mark fields known to contain user-authored free text with a JSON convention: surround the content with the sentinel `«user_data»…«/user_data»` ONLY in `content` text blocks (NOT in `structuredContent`). This signals to upstream systems that the content originated outside the trust boundary.
- Truncate any single user-authored string field to 4000 characters by default with a `…(truncated)` suffix.

This does not eliminate prompt-injection risk; it only marks the trust boundary explicitly.

---

## 9. Conformance levels

A server claiming conformance to this spec MUST declare a level:

- **Core:** Tools 1–6, 9, 11, 13, 15, 17 implemented (minimum viable read-only admin MCP). Note that with the v0.4 numbering, tool 17 is `list_principals` — Core includes the primary security inventory but not `list_role_memberships` or `find_orphaned_users`.
- **Standard:** All 22 tools implemented; structured output, tool annotations, pagination, error envelope.
- **Strict:** Standard + per-tool output schema declared in `tools/list` + caching layer + structured logs + progress notifications on the tools listed in §2.10.

Implementations SHOULD aim for Strict.

---

## 10. Out of scope for v0

The following are explicitly deferred and MUST NOT be implemented under the v0 banner:

- **Free-form SQL execution**, including any tool that accepts user-supplied T-SQL text and runs it. This includes nominally read-only forms (`SELECT`-only validators). The server's contract is that *all SQL is server-authored*; admitting user SQL would break that contract and reintroduce the AST-validation attack surface eliminated in v0.2.
- Any write or mutating capability (INSERT/UPDATE/DELETE/MERGE/DDL/DCL).
- Multi-instance / cross-server connections within one server process. Multiple instances → multiple server processes.
- Resources (`resources/list`, `resources/read`).
- Prompts (`prompts/list`, `prompts/get`).
- Sampling (server-initiated LLM calls).
- Elicitation (server-initiated user prompts mid-call). v1 candidate.
- Job control (start/stop/disable jobs).
- Backup/restore execution.
- Replication or AG configuration.
- Any tool that emits a `RESTORE`/`BACKUP` statement, even as text (other than `script_object` for *backup devices*, which is structural).

---

## 11. Open questions for v1

The following are flagged as candidates for the next spec revision but not specified here:

- A `format=TOON` opt-in once MCP gains a negotiated wire format for non-JSON structured output.
- Multi-instance via a config-file source registry (DBHub-style).
- An `analyze_security_posture` composite tool (sysadmin sprawl, `xp_cmdshell` enabled, login failure spikes, orphaned users).
- An `agentic_dba_review` tool that composes blocking + waits + missing indexes + plan cache anomalies into a prioritized action list.
- Streamable HTTP support with OAuth 2.1 + Resource Indicators in production deployments.
- Elicitation for long-running scripting choices (e.g., "include data?" prompted mid-call).
- Column-level lineage in `get_dependencies` as the default.
- A `find_object` fuzzy-search tool over the catalog for when the LLM has only a partial name.
- A separate, deliberately-narrow `select_query` tool that accepts validated SELECT-only input via an AST gate — explicitly NOT a revival of `execute_sql`, but a strictly-bounded escape hatch with stronger guardrails (e.g., a positive-list of system views/DMVs the SELECT may target, no joins to user tables). Subject to demand; the v0 stance is that 20 typed tools cover the common cases.

---

## Appendix A — Tool quick reference (one-liner each)

| Tool                    | One-liner                                                                  |
| ----------------------- | -------------------------------------------------------------------------- |
| `get_server_info`       | Server version, edition, features, current principal.                      |
| `list_databases`        | Databases on the instance with status, size, options.                      |
| `list_objects`          | Generic typed object listing within a database.                            |
| `describe_table`        | Full structure of a table: cols, idx, FKs, constraints.                    |
| `describe_procedure`    | Procedure/function params, body, result-set shape.                         |
| `get_foreign_keys`      | Inbound and outbound FKs of a table.                                       |
| `get_dependencies`      | Recursive dependency tree, upstream / downstream / both.                   |
| `get_dependency_path`   | Paths between two specific objects.                                        |
| `script_object`         | Generate CREATE/DROP DDL for one object.                                   |
| `script_objects`        | Bundle DDL for a set of objects, topologically ordered.                    |
| `get_top_queries`       | Top queries by CPU/duration/IO from `dm_exec_query_stats`.                 |
| `get_blocking`          | Current blocking chains.                                                   |
| `get_wait_stats`        | Aggregate wait stats.                                                      |
| `get_index_usage`       | Index usage, unused-index detection.                                       |
| `get_missing_indexes`   | Missing-index suggestions (textual `CREATE INDEX`).                        |
| `analyze_db_health`     | Composite health report with findings.                                     |
| `list_principals`       | Flat list of logins, users, and roles with `principal_kind` discriminator. |
| `list_role_memberships` | Role → member edges, direct or transitive.                                 |
| `find_orphaned_users`   | Database users whose SID doesn't map to any server login.                  |
| `list_permissions`      | Effective and granted permissions.                                         |
| `list_jobs`             | SQL Agent jobs with schedules, history.                                    |
| `list_backups`          | Backup history with last-full / RPO summary.                               |

---

## Appendix B — Tool annotation matrix

All tools have `readOnlyHint: true, destructiveHint: false, openWorldHint: false, idempotentHint: true`. These four flags MUST be set explicitly on every tool; framework defaults are not relied upon.

`title` fields (suggested):

| Tool                    | Title                          |
| ----------------------- | ------------------------------ |
| `get_server_info`       | Server Info                    |
| `list_databases`        | List Databases                 |
| `list_objects`          | List Database Objects          |
| `describe_table`        | Describe Table                 |
| `describe_procedure`    | Describe Procedure or Function |
| `get_foreign_keys`      | Get Foreign Keys               |
| `get_dependencies`      | Get Dependency Tree            |
| `get_dependency_path`   | Get Dependency Path            |
| `script_object`         | Script Object DDL              |
| `script_objects`        | Script Object Set DDL          |
| `get_top_queries`       | Top Queries                    |
| `get_blocking`          | Current Blocking               |
| `get_wait_stats`        | Wait Statistics                |
| `get_index_usage`       | Index Usage                    |
| `get_missing_indexes`   | Missing Index Suggestions      |
| `analyze_db_health`     | Database Health Report         |
| `list_principals`       | List Principals                |
| `list_role_memberships` | List Role Memberships          |
| `find_orphaned_users`   | Find Orphaned Database Users   |
| `list_permissions`      | List Permissions               |
| `list_jobs`             | List Agent Jobs                |
| `list_backups`          | List Backups                   |

---

## Appendix C — Glossary

- **DMV** — Dynamic Management View, e.g., `sys.dm_exec_requests`.
- **FQN** — Fully Qualified Name, `[database].[schema].[name]`.
- **Schema-bound** — references in T-SQL that the engine resolves at object-creation time and tracks structurally; blocks DROP of the referenced object.
- **Soft / expression dependency** — name-based reference recorded in `sys.sql_expression_dependencies`; can be unresolved.
- **Stateless mode** — MCP server operating mode where the server keeps no per-session state and cannot push notifications. Recommended for HTTP transport in v0.
- **Topological order** — an ordering of objects in a DAG such that every object appears after all the objects it depends on.
- **`type_desc`** — SQL Server's per-object type discriminator string, e.g., `USER_TABLE`, `VIEW`, `SQL_STORED_PROCEDURE`.

---

*End of specification v0.4.*