---
version: "0.1.2"
level: pair
processes:
    design: pair
    implementation: pair
    testing: copilot
    documentation: copilot
    review: assist
    deployment: assist
---

# AI Declaration

This file follows the [AI Declaration Standard v0.1.2](https://ai-declaration.md/en/0.1.2/).

## Notes

`net-ssms-mcp` is an MCP server that connects to Microsoft SQL Server instances with real credentials and exposes their metadata to an AI agent. Anyone pointing it at a database deserves a straight answer about how it was built. This file is that answer.

### What we use AI for

- **Anthropic Claude** (Opus), via the **Claude Code** CLI.
- **GitHub Copilot**, for automated pull request review.
- Editor and CLI plugins: `microsoft-docs` (official Microsoft documentation lookup), `csharp-lsp`, and `superpowers-extended-cc`.

The repository is also configured *for* AI agents: `CLAUDE.md`, `.claude/rules/` and an `AGENTS.md` in every project exist so that assistants working here follow the same conventions a human contributor would.

### How AI is used

The declared level per process reflects how this project is actually built:

- **Design - pair:** `docs/SPEC.md` is drafted with AI and iterated across versions, but scope, conformance levels and every trade-off are human calls.
- **Implementation - pair:** AI drafts the C# under `src/`; the human shapes the result and retains a clear understanding of the internals. Architecture is not delegated.
- **Testing - copilot:** the suites under `tests/` are largely generated from established patterns, then reviewed. Tests run against real SQL Server 2022 and 2025 containers, not against an AI's description of what they would do.
- **Documentation - copilot:** `CLAUDE.md`, `.claude/rules/` and the `AGENTS.md` set are predominantly AI-authored from human direction, then reviewed and corrected.
- **Review - assist:** changes land through pull requests. GitHub Copilot performs an automated review pass, and a human reviews on top of it. AI comments; it does not approve or merge.
- **Deployment - assist:** CI workflows, container definitions and release plumbing are human-owned, with AI help on parts.

### Human review is non-negotiable

- AI output is treated as a draft, not a commit. Every commit is authored and signed by a human.
- Factual claims produced by AI — API names, method signatures, file paths, package versions — are verified against the actual source or official documentation before they land. This is a real failure mode, not a theoretical one: AI-drafted guidance in this repository has previously referenced types and paths that did not exist, and was corrected on review.
- Security-relevant code (connection handling, credential and secret sourcing, identifier handling, anything touching SQL text) receives additional human scrutiny.

### Read-only is a design guarantee, not a suggestion

The server is **read-only by construction** (`docs/SPEC.md` §2.1): all SQL is server-authored from system catalogs and DMVs, free-form SQL execution was deliberately removed from the spec, and enforcement rests on connection-level least privilege plus server-authored SQL only. All tools declare `ReadOnly = true`, `Destructive = false`.

No AI-drafted change gets to widen that surface. Adding write capability is a human product decision, and the spec currently defers it indefinitely.

### Expectations for contributors

If you use AI to help write a contribution, please declare it. The pull request template asks you to confirm you have reviewed this file and updated it if your contribution changes how AI is used here.

AI-assisted contributions are welcome, disclosure is expected, and you remain responsible for what you submit.

### Why we publish this

Publishing an AI declaration is about trust. This tool is pointed at databases that matter, and its guarantees — read-only, least privilege, no free-form SQL — are only worth as much as the process that produced them. Users, auditors and contributors are entitled to know how that process actually works.

If anything here changes — new tools, new workflows, different levels — this file is updated and the declaration version bumped.

---

*Questions about this declaration? Please [open an issue](https://github.com/Herve-M/net-ssms-mcp/issues).*
