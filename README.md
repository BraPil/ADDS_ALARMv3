# ADDS_ALARMv3

ALARMv3 analysis workspace for the ADDS legacy codebase.

**Source repo**: [BraPil/ADDS_Orig](https://github.com/BraPil/ADDS_Orig)  
**ALARMv3**: [BraPil/ALARMv3](https://github.com/BraPil/ALARMv3)

---

## What this repo contains

This is an ALARMv3 *workspace* — the artifact zone for a live modernization engagement.
All analysis databases, learned grammars, and human-readable reports live here.
The source codebase (`ADDS_Orig`) is **never modified**.

```
.alarmv3/
├── session.db          # Session state and work queue
├── memory.db           # Learned language grammars (AutoLISP, DCL, PowerShell)
│                         persisted across sessions — future runs skip Claude inference
└── sessions/
    └── 32552d53.../
        ├── README.md               ← start here
        ├── evaluation_report.md    ← 14 recommendations + adversarial verdicts
        ├── implementation_plan.md  ← 14-step dependency-ordered plan
        ├── symbol_index.md         ← all 124 symbols across all languages
        ├── learned_grammars.md     ← runtime-learned AutoLISP/DCL/PowerShell syntax
        ├── recommendations.md      ← plain recommendation list
        ├── manifest.json           ← file inventory
        ├── summary.json            ← session statistics
        ├── analysis.db             ← full semantic graph (SQLite)
        └── audit.log               ← WORM action log
```

---

## Current status

| Stage | Status |
|-------|--------|
| File discovery | ✅ 19 files (7 C#, 6 AutoLISP, 2 PS1, 1 DCL, 1 PSM1, 1 SQL, 1 config) |
| Language research | ✅ AutoLISP, DCL, PowerShell grammars learned + persisted |
| Symbol analysis | ✅ 124 symbols extracted |
| Synthesis | ✅ 14 recommendations generated |
| Adversarial evaluation | ✅ 4 accept / 10 revise / 0 reject |
| Human review | ✅ All 14 approved |
| Implementation planning | ✅ 14-step plan, dependency-ordered |
| Implementation | ✅ 14/14 changes applied — see `modernized` branch |

---

## Asking questions about ADDS

Once the ALARMv3 MCP server is connected to your AI assistant (Claude Desktop, Cursor, etc.),
you can ask plain-English questions about the codebase. The agent uses the `query_codebase`
tool and the semantic graph in `analysis.db` to answer — no need to read raw source files.

### Connect ALARMv3 to your MCP client

```json
"alarmv3": {
  "command": "uv",
  "args": ["run", "alarmv3-mcp"],
  "cwd": "/path/to/ALARMv3",
  "env": {
    "ALARMV3_WORKSPACE": "/path/to/ADDS_ALARMv3",
    "ANTHROPIC_API_KEY": "sk-ant-..."
  }
}
```

Then just talk to your AI assistant. Examples of what you can ask:

---

### Security and credentials

> *"What databases are involved and what are the credentials used to access them?"*

The agent will query the dependency graph and symbol index for Oracle connection strings,
hardcoded credentials, and config files. For ADDS it will surface:

- `Oracle 11g` at `ORACLE11G-PROD:1521/ADDSDB`
- Username `adds_user`, password `adds_p@ss_2003!` hardcoded in:
  `OracleConnection.cs`, `deploy-adds.ps1`, `sync-oracle.ps1`, `ADDSOracleModule.psm1`, `adds.config`
- The `ADS_OADB` ODBC DSN used by AutoLISP (`pipe-routing.lsp`, `equipment.lsp`)

---

### Architecture and structure

> *"What are the main subsystems in ADDS and how do they interact?"*

> *"Which AutoLISP functions write to the database, and which functions do they call?"*

> *"What is the entry point for a user placing a pump in a drawing?"*

The agent walks the dependency graph: `C:ADDS-PLACE-PUMP` → `adds-insert-equipment-block`
+ `adds-db-save-equipment` → `ads_oadb_execute` (Oracle OADB bridge).

---

### Cross-implementation comparisons

> *"How do the APC Transmission, APC Distribution and GPC Transmission implementations differ?"*

> *"Which modules are shared between substation types and which are unique?"*

> *"Where is the single-line diagram logic for each transmission type, and do they share a common base?"*

The agent compares symbol tables, dependency edges, and file groupings across the
subsystems identified during deep analysis to find divergence points and shared code.

---

### Modernization impact

> *"Which files changed the most between the original and modernized branch?"*

> *"What was removed from OracleConnection.cs in the ODP.NET migration?"*

> *"Are there any AutoLISP files that still make direct Oracle calls after the migration?"*

The agent can diff specific files between `ADDS_Orig/main` and `ADDS_ALARMv3/modernized`,
or query `implementation_changes.md` for a summary of every applied change.

---

### Deep-dive queries

> *"List every function with cyclomatic complexity above 10."*

> *"Which files have the most inbound dependencies — what would break first?"*

> *"Show me all SQL strings that are built by string concatenation — potential injection points."*

These are answered directly from the `complexity_metric`, `dependency_edge`, and `symbol`
tables in `analysis.db` without any additional Claude calls.

---

### How it works under the hood

```
Your question
    ↓
AI assistant (Claude / Cursor)
    ↓  calls
query_codebase(session_id, question="...")   ← MCP tool
    ↓  queries
analysis.db  (symbols, deps, complexity, manifest, recommendations)
    ↓  returns ranked results
AI assembles answer from semantic graph data
```

The LLM never reads raw source files for Q&A — it reads the pre-built semantic graph,
which makes answers fast and consistent regardless of codebase size.

---

## How to use agentically (full pipeline)

### 1. Resume or start a new session

Point `ALARMV3_WORKSPACE` at this directory. Tell your AI assistant:

> "The ADDS modernization workspace is at ADDS_ALARMv3. The session is complete —
> walk me through the implementation changes and show me what was modernized."

### 2. Tools the agent uses for implementation

```
clone_for_implementation(session_id, target_path="<where to write modernized code>")
implement_batch(session_id, max_concurrent=3)
# For each change surfaced:
accept_change(session_id, change_id)   # commits to target
reject_change(session_id, change_id, feedback="...")
```

### 3. Resources the agent reads

| Resource URI | What it returns |
|---|---|
| `session://current` | Session state, source path, current stage |
| `recommendations://evaluated` | All 14 recommendations with evaluator verdicts |
| `implementation://plan` | The 14-step ordered plan |
| `implementation://changes` | Diffs pending review |
| `manifest://files` | Full file inventory |

---

## Key findings (from evaluation_report.md)

| # | Severity | Title | Verdict |
|---|----------|-------|---------|
| 1 | 🔴 CRITICAL | Replace unmanaged ODP.NET 11g with managed ODP.NET 19c | 🔄 revise |
| 2 | 🔴 CRITICAL | Migrate .NET Framework 4.5 → .NET 10 | 🔄 revise |
| 3 | 🟠 HIGH | Parameterize all Oracle calls (SQL injection, 6+ locations) | 🔄 revise |
| 4 | 🟠 HIGH | Externalize hardcoded Oracle credentials | 🔄 revise |
| 5 | 🟠 HIGH | Replace AutoLISP COM layer with AutoCAD .NET API | 🔄 revise |
| 6 | 🟠 HIGH | Replace WinForms with AutoCAD palette / WPF | 🔄 revise |
| 7 | 🟠 HIGH | Introduce async/await for all DB and sync ops | 🔄 revise |
| 8 | 🟠 HIGH | Introduce repository pattern | ✅ accept |

See [evaluation_report.md](.alarmv3/sessions/32552d53-4281-4c3c-a922-4dd1ad53be8c/evaluation_report.md) for the full list.
