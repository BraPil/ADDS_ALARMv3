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
| Implementation | ⏳ Not yet started |

---

## How to use agentically

ALARMv3 is an MCP server. An AI agent (Claude, Cursor, etc.) drives it — not a human CLI.

### 1. Add to your MCP client config

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

### 2. Resume the existing session

The session is in state `IMPLEMENTATION_PLANNED`. Tell your AI assistant:

> "Resume the ADDS modernization session. The workspace is at ADDS_ALARMv3.
> Clone the repo for implementation, then run implement_batch and walk me
> through each change."

### 3. The agent will call these tools in order

```
clone_for_implementation(session_id, target_path="<where to write modernized code>")
implement_batch(session_id, max_concurrent=3)
# For each change surfaced:
accept_change(session_id, change_id)   # commits to target
reject_change(session_id, change_id, feedback="...")
```

### 4. Resources the agent reads

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
