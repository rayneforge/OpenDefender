# Contributing

OpenDefender helps an agent explain local device security and health while the
owner controls changes. Keep additions small, readable, and useful to someone who
does not administer security systems professionally.

## Before proposing a change

Open a feature issue with the user question, evidence needed, platform coverage,
permissions, and privacy implications. Report vulnerabilities privately using
[SECURITY.md](SECURITY.md). Sanitize all logs and examples.

## Development

Install the .NET 10 SDK, then run:

```bash
dotnet build src/ObservabilityStack.slnx -c Release
dotnet test src/Tests/Tests.csproj -c Release --no-build --filter "Category=WindowsCommand|Category=Process|Category=NetworkConnection"
```

On Linux use `LinuxCommand` instead of `WindowsCommand`. Use PowerShell 7 for
`scripts/Test-McpStdio.ps1`; publish and package validation examples are in the
[README](README.md). LLM evaluation tests require separately configured providers
and are not part of the default deterministic suite.

## Design expectations

- No implicit elevation, interactive privilege prompts, arbitrary shell execution,
  or automatic remediation.
- Distinguish unavailable evidence from healthy results. Include freshness and scope.
- Avoid recording packet payloads, credentials, or unnecessary identifying data.
- Keep shell probes bounded and cancellable. Test failure paths and IPv4/IPv6 parsing.
- Mark MCP query tools read-only, non-destructive, and local where accurate.
- Explain flags with evidence and an owner-controlled next step. Do not equate
  a threshold flag with compromise or a listening socket with internet exposure.
- Include meaningful tests and update the user-facing coverage documentation.

PRs should describe the observable change, required permissions, data retained,
and validation on each relevant OS. Keep platform-specific limitations explicit.
