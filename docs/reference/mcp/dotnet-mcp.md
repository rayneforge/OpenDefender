# .NET MCP Server Reference

Complete reference for building MCP servers in C# using the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) with ASP.NET Core transport.

---

## Overview

MCP exposes three server primitives:

| Primitive    | Purpose                    | Protocol Method  | DDD Analogy          |
| ------------ | -------------------------- | ---------------- | -------------------- |
| **Tool**     | Execute logic/side effects | `tools/call`     | Command              |
| **Resource** | Provide structured data    | `resources/read` | Query                |
| **Prompt**   | Reusable prompt templates  | `prompts/get`    | Policy / Instruction |

---

## Contents

| Topic | File |
|-------|------|
| Packages, transport, server wiring, handlers, session, checklist | [setup.md](setup.md) |
| Tool definition, registration, interception | [tools.md](tools.md) |
| Resource templates, list, read, subscriptions | [resources.md](resources.md) |
| Prompt definition and registration | [prompts.md](prompts.md) |
| JWT auth, scopes, authorization filters | [auth.md](auth.md) |
| Testing with MCP + Microsoft.Extensions.AI | [testing.md](testing.md) |
