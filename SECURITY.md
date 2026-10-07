# Security policy

OpenDefender is an early-stage, read-only observability MCP server. Use the latest
release; older versions do not have a separate security maintenance branch.

## Report a vulnerability

Use [GitHub private vulnerability reporting](https://github.com/rayneforge/OpenDefender/security/advisories/new).
Do not post exploit details, credentials, device telemetry, or private addresses in
a public issue. Include the affected version, OS, reproduction steps, expected
boundary, and a sanitized description of the impact. No response SLA is promised.

## Intended boundary

- Run under the owner's ordinary account, without automatic elevation.
- Use fixed read probes and typed query inputs, never agent-supplied shell commands.
- Default to local stdio, with no HTTP listener or required LLM service.
- Do not modify host configuration or automatically remediate findings.
- Store only local telemetry with limited retention; do not persist live connection snapshots.
- Treat tool output as untrusted evidence. Read-only annotations do not enforce isolation.

The connected MCP client may send results to its AI provider. Enabling optional
built-in LLM agents also changes the data-sharing boundary. The optional HTTP API
has no built-in authentication and is intended for localhost development only.

Known limitations include inconsistent legacy probe error reporting, partial OS
coverage, and heuristic analytics. A clean or empty report is not a guarantee of
security. See the [roadmap](docs/design/roadmap.md).
