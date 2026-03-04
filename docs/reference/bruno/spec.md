# Bruno v3.1+ (YAML-first) — Engineering Spec + Postman 1:1 Mapping

> Target baseline: Bruno **v3.1.4** (release Feb 24, 2026). YAML is the default format starting **v3.1.0**. OpenCollection YAML support starts **v3.0.0**.

---

## 1) Purpose

This spec defines a **repo-native, YAML-first** way to use Bruno as:

* a **Postman replacement** (interactive API exploration + tests)
* a **Git-native API test harness** (reviewable diffs, mergeable changes)
* an **agent-consumable API interaction artifact** (YAML is parseable; can generate tool schemas)

---

## 2) Definitions

* **Bruno**: offline-first API client with Git-native collections.
* **Collection**: a folder on disk containing request artifacts + env artifacts.
* **OpenCollection YAML**: the YAML schema Bruno uses to store requests by default (v3.1+).
* **Request artifact**: a `.yml/.yaml` file representing one request (REST/GraphQL/SOAP/gRPC/WebSocket).

---

## 3) Compatibility Targets

### 3.1 Platforms

* Linux desktop (AppImage/Flatpak/AUR) + CLI runner
* CI runner (Linux) using `bru` CLI

### 3.2 Protocol coverage

Bruno supports (current docs):

* REST
* GraphQL
* SOAP
* gRPC
* WebSocket

> Note: some protocol features may be “beta” depending on release line and preferences toggles.

---

## 4) Repo Storage Model (Git Contract)

### 4.1 Required properties

* **Collections are folders** in your repo (no cloud workspace required).
* **Requests are YAML files** by default in v3.1+.
* **Secrets are not committed**: use `.env` / CI secret injection.

### 4.2 Recommended repo layout

```
repo/
  api/
    openapi/
      service-a.yaml
  api-tests/
    collections/
      service-a/
        opencollection.yml
        environments/
          local.yml
          prod.yml
        requests/
          users/
            get.users.yml
            create.user.yml
          health/
            health.yml
        .env.example
        .gitignore
  .github/workflows/
    api-tests.yml
```

### 4.3 Git policy

* Treat request YAML as **code**:

  * PR review required
  * lint/validate in CI
  * disallow committing `.env` with secrets

---

## 5) Collection Manifest + Metadata

Bruno collections include a manifest file (commonly `bruno.json` or `opencollection.yml`) that defines collection-level metadata and behavior.

**Spec requirement:**

* `bruno.json` OR `opencollection.yml` MUST exist at collection root.
* Collection folders MUST be movable and runnable by the CLI.

### 5.1 OpenCollection Manifest Structure

When using `opencollection.yml`, collection-level variables MUST be defined under a `request` block to be globally accessible:

```yaml
opencollection: 1.0.0
info:
  name: My Collection
request:
  variables:
    - name: baseUrl
      value: http://localhost:3000
      enabled: true
```

---

## 6) OpenCollection YAML — Canonical Request Schema (v3.1+ default)

### 6.1 Top-level sections

Each request YAML file SHOULD use these top-level keys:

* `info`      — metadata (name, type, seq, tags)
* `http`      — HTTP request configuration (or protocol-equivalent)
* `runtime`   — scripts and assertions
* `settings`  — request settings
* `docs`      — request documentation

### 6.2 Minimal viable request (MVY)

A request file MUST contain enough information to execute:

* `info.name`
* `info.type`
* protocol section depending on type:

  * HTTP/REST/GraphQL/SOAP: `http.method`, `http.url`
  * WebSocket: ws URL + headers
  * gRPC: target, proto refs, method path

### 6.3 Naming + determinism

* Filename SHOULD use dot-notation: `get.users.yml` (preferred over kebab-case).
* `info.name` SHOULD be human-friendly
* Tool/agent name derivation SHOULD be deterministic:

  * `toolName = normalize(filename)` OR `normalize(info.name)`

### 6.4 Tags

Requests SHOULD use tags for routing (CI, smoke, slow, etc):

* `smoke`
* `integration`
* `skip-ci`

---

## 7) Variables & Environments (Postman parity)

Bruno defines multiple variable scopes.

### 7.1 1:1 mapping

| Postman concept       | Bruno equivalent                   | Notes                |
| --------------------- | ---------------------------------- | -------------------- |
| Environment variables | Environment variables              | dev/prod sets        |
| Global variables      | Global environments / global scope | depends on setup     |
| Collection variables  | Collection variables               | stored at collection |
| Folder variables      | Folder variables                   | inherits down        |
| Request variables     | Request variables                  | local to request     |
| Runtime variables     | Runtime variables                  | computed during run  |
| Secret variables      | Process env / `.env` / CI secrets  | DO NOT COMMIT        |

### 7.2 Secret handling policy

* Use a root `.env` (ignored by git)
* Commit a `.env.example` with placeholder keys

`.gitignore` MUST include:

* `.env`
* `*.local.*`
* any file containing secrets

---

## 8) Authentication (Postman parity)

### 8.1 Auth types you should standardize for internal APIs

* API Key
* Bearer token
* OAuth2 (client credentials / auth code)
* Basic auth

### 8.2 Spec requirement

* Auth MUST be defined in request YAML OR inherited via folder/collection conventions.
* Token acquisition SHOULD be modeled as explicit requests:

  * `auth/token.yaml` produces runtime vars

### 8.3 Recommended pattern: auth bootstrap

```
requests/
  auth/
    get-token.yaml     # retrieves token
  users/
    get-users.yaml     # uses {{token}}
```

---

## 9) Pre-request / Post-response scripting + Tests

Bruno supports JavaScript scripting for:

* pre-request logic
* post-response processing
* assertions
* tagging/conditional skip in runners

### 9.1 1:1 mapping

| Postman             | Bruno                                    |
| ------------------- | ---------------------------------------- |
| Pre-request scripts | Pre-request scripting (runtime)          |
| Tests tab           | Post-response tests/assertions           |
| pm.* API            | bruno JS API (req/res + runner controls) |
| Newman execution    | `bru run` execution                      |

### 9.2 Spec requirement

* Each request SHOULD include at least one assertion:

  * status code
  * schema/shape validation (minimal)

### 9.3 Data-driven testing

Bruno CLI supports using CSV/JSON sources to drive request runs.

**Spec requirement:**

* For endpoints with many permutations, add a `data/` folder:

```
collection/
  data/
    users.csv
```

---

## 10) Collection Runner + CI (Postman runner parity)

### 10.1 1:1 mapping

| Postman runner | Bruno runner                 |       |
| -------------- | ---------------------------- | ----- |
| Run collection | `bru run <collection>`       |       |
| Run folder     | `bru run <folder>`           |       |
| Data file      | CSV/JSON data source support |       |
| CLI            | `newman`                     | `bru` |

### 10.2 CI contract

A CI pipeline MUST:

* install Bruno CLI
* run `bru run` against targeted collections
* produce artifacts (JUnit or JSON) if supported by your reporting approach

**Recommended stages**

* `smoke`: tagged subset
* `integration`: full run

---

## 11) Import/Export + OpenAPI (Postman parity)

### 11.1 1:1 mapping

| Postman                    | Bruno                        |
| -------------------------- | ---------------------------- |
| Import OpenAPI             | Import OpenAPI to collection |
| Import Postman collections | Import supported             |
| Export collections         | Export supported             |

### 11.2 Spec requirement

* OpenAPI is the contract source-of-truth.
* Bruno collections are the executable test artifact.

**Recommended workflow**

1. Generate OpenAPI from services
2. Import OpenAPI to generate/update collection skeleton
3. Add tests/assertions + token workflows
4. Run in CI

---

## 12) Non-HTTP protocol parity (important differences vs Postman)

### 12.1 WebSocket

* Bruno supports WebSocket requests, message types, and subprotocol headers.

**Spec requirement**

* Model WebSocket test flows as:

  * connect
  * send message(s)
  * assert on response message(s)

### 12.2 gRPC

* Bruno supports gRPC including streaming docs.
* Some scripting APIs for gRPC may still be evolving.

**Spec requirement**

* Store `.proto` files alongside collection (or in a shared repo folder).

### 12.3 SOAP

* SOAP supported via request tooling.

---

## 13) Collaboration Model (Postman team workspace parity)

### 13.1 What you get vs Postman

| Postman capability  | Bruno equivalent       | Notes                |
| ------------------- | ---------------------- | -------------------- |
| Team workspace      | Git repo + PR workflow | strongest model      |
| Roles/permissions   | Git permissions        | reuse your org model |
| Sync across devices | Git pull               | explicit, auditable  |
| Cloud history       | Git history            | deterministic        |

### 13.2 What you DON’T get (or differs)

* Postman-style SaaS collaboration (shared cloud workspace UI)
* Postman API Network/public discovery (unless you build it)
* Postman Monitors in the cloud (Bruno expects you to use CI/schedulers)

**Spec substitute for monitors**

* Use GitHub Actions scheduled workflows / cron jobs
* Or use your own scheduler (K8s CronJob, systemd timer)

---

## 14) Postman “Feature-by-Feature” Parity Checklist

Use this as a migration / adoption checklist.

### 14.1 Core API client

* [ ] REST requests
* [ ] Headers, query params
* [ ] Bodies: JSON, form-data, urlencoded
* [ ] File uploads
* [ ] Response viewer + timeline

### 14.2 Auth

* [ ] API Key
* [ ] Bearer
* [ ] OAuth2
* [ ] Token acquisition workflow

### 14.3 Testing

* [ ] Assertions per request
* [ ] Pre-request scripts
* [ ] Post-response scripts
* [ ] Data-driven tests

### 14.4 Runner + automation

* [ ] CLI runner in CI
* [ ] Tagged suites (smoke/integration)
* [ ] Scheduled runs (monitor replacement)

### 14.5 Specs + generation

* [ ] OpenAPI import
* [ ] OpenAPI drift detection

### 14.6 Advanced protocols (if needed)

* [ ] WebSocket
* [ ] gRPC
* [ ] SOAP
* [ ] GraphQL

---

## 15) “Agent-Relevant” Layer (why your stack cares)

### 15.1 Why YAML-first matters

With v3.1+, your request artifacts are **machine-readable YAML**. This enables:

* deterministic parsing
* tool schema generation
* automated test generation
* agent-driven execution

### 15.2 Agent ingestion contract

An agent that loads Bruno collections SHOULD:

1. scan `**/*.yaml` / `**/*.yml`
2. parse OpenCollection top-level keys
3. derive a stable tool name
4. expose an executable call that:

   * either replays the HTTP request directly
   * OR shells out to `bru run` for exact parity

### 15.3 Recommended: “execution parity mode”

For correctness, default to **`bru run`** as the execution engine.

* Pros: matches Bruno runtime, variables, scripts, assertions
* Cons: requires CLI availability

---

## 16) Migration Spec (Postman → Bruno)

### 16.1 Migration principles

* Keep OpenAPI as contract
* Convert Postman collections to Bruno (import)
* Normalize naming/tagging
* Recreate env management + token bootstrap

### 16.2 Migration steps

1. Export Postman collection(s)
2. Import into Bruno
3. Convert/standardize:

   * folder layout
   * request naming
   * environments
4. Add `smoke` tags
5. Add CI runner stage

---

## 17) Known Sharp Edges / Watch Items

* **Folder Naming**: Avoid spaces in folder names (e.g., use `Metrics` instead of `OData Metrics`). Spaces can prevent folders from rendering correctly in the UI.
* **Variable Scope**: In `opencollection.yml`, variables must be nested under `request.variables`, not at the top level.
* During the v3 transition, mixed-format artifacts can appear depending on import/export and actions; enforce repo policy checks if you want “YAML-only”.
* Some newer protocol integrations (notably gRPC scripting APIs) may evolve; treat them as version-gated.

---

## 18) Enforcement: “Bruno Collection Quality Gates”

Add a CI check that validates:

* Every request file has `info.name` and correct protocol section
* No `.env` committed
* All requests have at least one assertion
* Tags adhere to allowed set

---

## 19) What I need from you to finalize this spec to your exact installation

Drop the outputs of:

* `bruno --version` (desktop)
* `bru --version` (CLI)
* installation method (AppImage/Flatpak/AUR)

Then I’ll add a **Verified-On** section (Linux distro + package channel) and lock any version-gated notes.
