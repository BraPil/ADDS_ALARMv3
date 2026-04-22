# ADDS Codebase Wiki
> Last updated: 2026-04-22  
> All claims sourced directly from the original commit (`cf22e8d`) unless explicitly labelled [modernized].  
> Claims about domain purpose are separated from code facts — see §1.

---

## ⚠ Known analysis failures (read first)

Previous versions of this wiki contained systematic inaccuracies. Documented here so they are not repeated:

| Failure | Root cause |
|---|---|
| Domain description ("industrial plant design", "P&ID") was called out as wrong | No domain description exists in the code. Those labels were AI inference, not sourced from files. |
| LOC reported as "509" | 509 = ALARMv3's *net code lines* (non-blank, non-comment). Gross line count of original is 1,485. These are different metrics. |
| Functions like `adds-load-config` and `adds-sanitize-tag` described as original | These were added by ALARMv3 modernization. The original does not have them. |
| `ADDSOracleModule.psm1` omitted | This file exists in the original but was deleted in the modernized branch. |
| Vector search returns mid-function code bodies for domain queries | Chunking slices at symbol start/end lines — file-level headers (the most descriptive lines) fall before the first symbol and are not embedded. Queries about "what ADDS is" find code, not documentation. |

---

## 1. What is ADDS?

**ADDS — Automated Drawing & Design System**  
Source: `lisp/dialogs/main-menu.dcl:5` in original commit.

**What the code confirms:**
- An AutoCAD plugin (AutoLISP + C#) that lets users place named engineering objects into drawings via interactive commands
- All placed objects are persisted to an Oracle database with tag identifiers and coordinate data
- A background sync process keeps a local file cache synchronized with the Oracle data
- The system has been in use since at least 1991 (schema DDL creation date); AutoLISP files carry modification dates of 1997 and 2000

**What the code does NOT confirm:**
- The domain or industry the system serves — this is NOT stated anywhere in any source file, comment, or artifact in this repo
- Whether the named object types (pump, heat exchanger, vessel, etc.) accurately represent the real-world objects the system manages, or are representative placeholders

**What requires domain knowledge to answer (do not infer):**
- What type of drawings ADDS produces
- What business workflow ADDS supports
- Whether the equipment types reflect the real deployment

---

## 2. File Inventory

### Original codebase (`cf22e8d` — ground truth)

| File | Gross lines | Net code | Language | Role |
|---|---|---|---|---|
| `lisp/dialogs/dialog-utils.lsp` | 59 | — | AutoLISP | Entry point (`C:ADDS`), menu dispatch, settings |
| `lisp/dialogs/main-menu.dcl` | 88 | — | DCL | Main menu + settings dialog definitions |
| `lisp/drawing/draw-commands.lsp` | 68 | — | AutoLISP | Draw pipe/vessel/instrument; global state; logging |
| `lisp/drawing/equipment.lsp` | 85 | — | AutoLISP | Place pump/HX/tank; equipment report; delete |
| `lisp/drawing/pipe-routing.lsp` | 78 | — | AutoLISP | Route pipe; OADB connect/disconnect; list/edit pipes |
| `lisp/utils/db-utils.lsp` | 80 | — | AutoLISP | DB init, offline queue, flush, raw query |
| `lisp/utils/string-utils.lsp` | 55 | — | AutoLISP | Trim, upper, split, replace, format-tag, validate-tag |
| `csharp/AutoCAD/DrawingManager.cs` | 122 | — | C# | COM/ActiveX AutoCAD automation; DrawingManager + BlockLibraryManager (same file) |
| `csharp/AutoCAD/LayerManager.cs` | 58 | — | C# | Layer freeze/thaw/purge |
| `csharp/DataAccess/OracleConnection.cs` | 110 | — | C# | OracleConnectionFactory (singleton, unmanaged ODP.NET 11.2), EquipmentRepository, InstrumentRepository |
| `csharp/DataAccess/StoredProcedures.cs` | 84 | — | C# | StoredProcedureRunner, BulkDataLoader |
| `csharp/Forms/MainForm.cs` | 94 | — | C# | WinForms MainForm; UI-thread blocking sync; SQL injection in search |
| `csharp/Services/ProjectService.cs` | 45 | — | C# | OpenProject, GetProjectList, CreateProject |
| `csharp/Services/SyncService.cs` | 92 | — | C# | BackgroundWorker-based sync (no async) |
| `powershell/admin/ADDSOracleModule.psm1` | 99 | — | PowerShell | Connect-ADDSOracle, Invoke-ADDSOracleQuery, Backup/Restore-ADDSDatabase, Get-ADDSTableStats, Reset-ADDSSequences |
| `powershell/db/sync-oracle.ps1` | 106 | — | PowerShell | Nightly sync: equipment cache + instrument CSV; alerts |
| `powershell/deploy/deploy-adds.ps1` | 103 | — | PowerShell | Deploy prerequisites check, file copy, Oracle test, install service |
| `sql/schema.sql` | 50 | — | SQL | DDL: EQUIPMENT, PIPE_ROUTES, INSTRUMENTS, VESSELS + 2 sequences |
| `config/adds.config` | 9 | — | Config | Oracle host/port/SID/user/pass (plaintext), AutoCAD version, block library, log path, units |
| **Total original** | **1,485** | **~509** | | 19 files, 5 languages |

`~509` net code lines = ALARMv3 count excluding blank lines and comments (from `summary.json`).

### Differences in the modernized branch (main)

| Change | Detail |
|---|---|
| `ADDSOracleModule.psm1` **deleted** | No replacement; its backup/restore/stats functionality is gone |
| `BlockLibraryManager` **extracted** | Split out of `DrawingManager.cs` into its own file; functionality same |
| `draw-commands.lsp` **+28 lines** | `adds-load-config` function added (reads config file for Oracle globals); not in original |
| `equipment.lsp` / `pipe-routing.lsp` | `adds-sanitize-tag` added to both; not in original |
| `pipe-routing.lsp`: `adds-oadb-connect` | Original hardcodes `"adds_user"` / `"adds_pass_plaintext"`; modernized reads env vars |
| Oracle driver (C#) | Unmanaged `Oracle.DataAccess` 11.2 → managed `Oracle.ManagedDataAccess.Client` 19c |
| AutoCAD API (C#) | COM/ActiveX (`Autodesk.AutoCAD.Interop`) → native .NET (`AcMgd.dll`) |
| UI (C#) | WinForms `MainForm` → WPF `PaletteSet` MVVM |
| Async (C#) | `BackgroundWorker` → `async/await` + `CancellationTokenSource` |
| Modernized gross LOC | 1,810 (20 files) |

---

## 3. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  AutoCAD Process (AutoCAD 2018 per config; R14 origin)       │
│                                                             │
│  AutoLISP Layer                                             │
│  ┌────────────────────────────────────────────────────┐    │
│  │ C:ADDS → DCL menu → C:ADDS-DRAW-*                  │    │
│  │                    → C:ADDS-PLACE-*                 │    │
│  │                    → C:ADDS-ROUTE-PIPE              │    │
│  │                    → C:ADDS-EQUIPMENT-REPORT        │    │
│  │ db-utils: OADB init / offline queue / flush         │    │
│  └────────────────────────────────────────────────────┘    │
│       │ ads_oadb_connect/execute/query/fetchrow/disconnect   │
│       │ (Oracle 7/8 OADB ADS API — embedded in AutoCAD)     │
│                                                             │
│  C# Plugin (ADDS.dll)                                       │
│  ┌────────────────────────────────────────────────────┐    │
│  │ DrawingManager  — COM/ActiveX AutoCAD automation    │    │
│  │ BlockLibraryMgr — loads .dwg blocks from library    │    │
│  │ LayerManager    — layer setup/freeze/purge          │    │
│  │ MainForm        — WinForms UI; equipment grid        │    │
│  │ EquipmentRepository / InstrumentRepository           │    │
│  │ SyncService     — BackgroundWorker sync loop         │    │
│  └────────────────────────────────────────────────────┘    │
│       │ Oracle.DataAccess.Client (ODP.NET unmanaged 11.2)   │
└─────────────────────────────────────────────────────────────┘
                 │
    ┌────────────┴──────────────────┐
    │    Oracle 11g Database        │
    │  EQUIPMENT                    │
    │  PIPE_ROUTES                  │
    │  INSTRUMENTS (incl. readings) │
    │  VESSELS                      │
    │  HEAT_EXCHANGERS (no DDL)     │
    │  PROJECTS       (no DDL)      │
    └───────────────────────────────┘
                 │
    ┌────────────┴──────────────────────────────────┐
    │  ADDSSyncService (Windows Service)             │
    │  sync-oracle.ps1 (nightly Task Scheduler)      │
    │  ADDSOracleModule.psm1 (admin functions)       │
    └───────────────────────────────────────────────┘
```

---

## 4. Entry Point and Command Flow (original code)

```
AutoCAD startup: LISP files loaded in this dependency order:
  1. string-utils.lsp      (no deps)
  2. db-utils.lsp          (depends on adds-oadb-connect from pipe-routing.lsp)
  3. draw-commands.lsp     (sets global state; defines draw commands)
  4. pipe-routing.lsp      (defines adds-oadb-connect and routing commands)
  5. equipment.lsp         (defines placement commands)
  6. dialog-utils.lsp      (defines C:ADDS, depends on all above)

User types: ADDS
  → C:ADDS (dialog-utils.lsp:55)
      1. adds-db-init (db-utils.lsp:4)
           → adds-oadb-connect(host, port, sid)  ← defined in pipe-routing.lsp
           → ads_oadb_connect(conn-str, "adds_user", "adds_pass_plaintext")  ← hardcoded
           → sets *ADDS-DB-CONNECTION*, *ADDS-USER-NAME*
      2. adds-show-main-menu
           → load_dialog "adds-main-menu.dcl"
           → new_dialog "adds_main_menu"
           → action_tile registrations
           → start_dialog → returns integer choice
      3. adds-dispatch-menu-result(choice)
           1 → C:ADDS-DRAW-PIPE
           2 → C:ADDS-DRAW-VESSEL
           3 → C:ADDS-PLACE-PUMP
           4 → C:ADDS-PLACE-HEAT-EXCHANGER
           5 → C:ADDS-ROUTE-PIPE
           6 → C:ADDS-EQUIPMENT-REPORT
           7 → adds-db-flush-queue
           8 → adds-show-settings
```

**Load-order dependency note**: `adds-db-init` calls `adds-oadb-connect`, which is only defined in `pipe-routing.lsp`. If the LISP files are not loaded in the correct order, `C:ADDS` fails at the DB init step.

---

## 5. Oracle Connectivity (original)

### AutoLISP side — OADB (Oracle 7/8 ADS API)

```lisp
; pipe-routing.lsp:18 — ORIGINAL (credentials hardcoded)
(defun adds-oadb-connect (host port sid / conn-str)
  (setq conn-str (strcat "DSN=ADDS_ORACLE;HOST=" host ";PORT=" (itoa port) ";SID=" sid))
  (ads_oadb_connect conn-str "adds_user" "adds_pass_plaintext")
)
```

No parameterized queries — all SQL is string-concatenated. No injection mitigation in original.

### C# side — ODP.NET unmanaged 11.2

```csharp
// OracleConnection.cs — ORIGINAL
private const string PASS = "adds_p@ss_2003!";  // plaintext constant
private static OracleConnection _sharedConnection;  // singleton
```

All SQL is string-concatenated. `InstrumentRepository.GetInstrumentsByArea` and `MainForm.txtSearch_TextChanged` are direct injection vectors.

---

## 6. Oracle Database Schema (original sql/schema.sql)

```sql
-- Created: 1991, last modified: 2008
EQUIPMENT    (TAG pk VARCHAR2(20), TYPE, MODEL VARCHAR2(50), LOCATION VARCHAR2(200),
              CREATED_BY, CREATED_DATE, MODIFIED)

PIPE_ROUTES  (ROUTE_ID pk via SEQ_ROUTE, TAG VARCHAR2(20), SPEC VARCHAR2(30),
              START_PT VARCHAR2(100), END_PT VARCHAR2(100), LENGTH NUMBER, CREATED_DATE)

INSTRUMENTS  (TAG pk, INSTR_TYPE VARCHAR2(10), AREA VARCHAR2(20),
              POS_X NUMBER, POS_Y NUMBER,
              LAST_VALUE VARCHAR2(50), TIMESTAMP DATE,
              CREATED_BY, CREATED_DATE, UPDATED)

VESSELS      (TAG pk, CTR_X NUMBER, CTR_Y NUMBER, RADIUS NUMBER,
              CREATED_BY, CREATED_DATE)

Sequences: SEQ_ROUTE, SEQ_HX
```

**Missing from schema but referenced in code:**

| Table | Referenced in |
|---|---|
| `HEAT_EXCHANGERS` | equipment.lsp:45, ADDSOracleModule.psm1 |
| `PROJECTS` | ProjectService.cs:26 |

**Missing from schema but referenced in code:**

| Sequence | Referenced in |
|---|---|
| `SEQ_INSTRUMENT` | ADDSOracleModule.psm1:`Reset-ADDSSequences` |

**Schema issues:** no FK constraints, no indexes on non-PK columns, coordinates stored as strings (`LOCATION VARCHAR2(200)`).

---

## 7. AutoLISP Command Reference (original)

| Command | File:Line | What the code does |
|---|---|---|
| `C:ADDS` | dialog-utils.lsp:55 | DB init + show main menu + dispatch |
| `C:ADDS-DRAW-PIPE` | draw-commands.lsp:5 | getpoint×2 + getreal → LAYER + LINE commands → log event to file |
| `C:ADDS-DRAW-VESSEL` | draw-commands.lsp:16 | getpoint + getreal + getstring → LAYER + CIRCLE + TEXT → `adds-db-save-vessel` |
| `C:ADDS-DRAW-INSTRUMENT` | draw-commands.lsp:43 | getpoint + getstring×2 → INSERT INSTR-{type} block + ATTDEF → `adds-db-save-instrument` |
| `C:ADDS-PLACE-PUMP` | equipment.lsp:4 | getpoint + getstring → INSERT PUMP-CENTRIFUGAL block → `adds-db-save-equipment` |
| `C:ADDS-PLACE-HEAT-EXCHANGER` | equipment.lsp:33 | getpoint + getstring×3 + getreal → INSERT HX-SHELLTUBE block → `adds-db-save-hx` |
| `C:ADDS-PLACE-TANK` | equipment.lsp:51 | getpoint + getstring×2 + getreal → CIRCLE (radius from capacity formula) → `adds-db-save-equipment` |
| `C:ADDS-ROUTE-PIPE` | pipe-routing.lsp:4 | getpoint×2 → orthogonal 2-segment LINE route → `adds-db-save-route` |
| `C:ADDS-LIST-PIPES` | pipe-routing.lsp:60 | SELECT TAG,SPEC,LENGTH FROM PIPE_ROUTES → print to prompt |
| `C:ADDS-EDIT-PIPE` | pipe-routing.lsp:72 | getstring → UPDATE PIPE_ROUTES SET MODIFIED=SYSDATE |
| `C:ADDS-EQUIPMENT-REPORT` | equipment.lsp:64 | SELECT TAG,TYPE,MODEL,CREATED_DATE FROM EQUIPMENT → print to prompt |
| `C:ADDS-DELETE-EQUIPMENT` | equipment.lsp:75 | getstring + Y/N confirm → DELETE FROM EQUIPMENT (no injection guard in original) |

---

## 8. Global State (original draw-commands.lsp)

```lisp
;; Set at load time — no config file reading in original
(setq *ADDS-DRAWING-SCALE*    1.0)
(setq *ADDS-CURRENT-PROJECT*  nil)
(setq *ADDS-DB-CONNECTION*    nil)   ; set by adds-db-init
(setq *ADDS-USER-NAME*        "")    ; set by adds-db-get-username
(setq *ADDS-UNIT-SYSTEM*      "IMPERIAL")
(setq *ADDS-LAYER-PREFIX*     "ADDS-")
(setq *ADDS-ORACLE-HOST*      "ORACLE11G-PROD")   ; hardcoded default
(setq *ADDS-ORACLE-PORT*      1521)
(setq *ADDS-ORACLE-SID*       "ADDSDB")
```

**No runtime config loading in original** — `adds-load-config` was added by ALARMv3 modernization and does not exist in the original code.

---

## 9. Offline Write Queue

When `*ADDS-DB-CONNECTION*` is nil, `adds-db-save-vessel` and `adds-db-save-instrument` fall back to appending raw SQL INSERT strings to `C:\ADDS\db_queue.sql`.

`adds-db-flush-queue` (menu: "Sync Database") replays all queued lines via `ads_oadb_execute` then deletes the file.

**Risk in original**: no sanitization of tag values before building SQL strings — direct injection vector. The `adds-sanitize-tag` whitelist function was added by ALARMv3 and is not in the original.

---

## 10. PowerShell Admin Module (original, deleted in modernized)

`powershell/admin/ADDSOracleModule.psm1` — present in original, **removed** by ALARMv3 modernization with no replacement.

Exported functions:

| Function | What it does |
|---|---|
| `Connect-ADDSOracle` | Opens ODP.NET 11.2 connection; credentials hardcoded as defaults |
| `Invoke-ADDSOracleQuery` | Executes raw SQL query, returns DataTable |
| `Backup-ADDSDatabase` | Runs Oracle `exp` via `Invoke-Expression` (security risk); hardcoded password in command line |
| `Restore-ADDSDatabase` | Runs Oracle `imp` via `Invoke-Expression` |
| `Get-ADDSTableStats` | SELECT COUNT(*) for EQUIPMENT, INSTRUMENTS, PIPE_ROUTES, VESSELS, HEAT_EXCHANGERS |
| `Reset-ADDSSequences` | ALTER SEQUENCE RESTART for SEQ_ROUTE, SEQ_HX, SEQ_INSTRUMENT |

---

## 11. Security Findings (original code)

| Finding | Severity | Location |
|---|---|---|
| Plaintext password in source (`"adds_pass_plaintext"`, `"adds_p@ss_2003!"`) | Critical | pipe-routing.lsp:21, OracleConnection.cs:28, ADDSOracleModule.psm1:6 |
| Same password in `config/adds.config` | Critical | config/adds.config:5 |
| SQL injection: MainForm search box concatenates raw user input | Critical | MainForm.cs:87 |
| SQL injection: InstrumentRepository.GetInstrumentsByArea concatenates area param | High | OracleConnection.cs:91 |
| SQL injection: all AutoLISP db-save functions build SQL via strcat; no sanitization | High | equipment.lsp, db-utils.lsp |
| Offline queue replays raw SQL at flush; no re-validation | High | db-utils.lsp:51 |
| Backup-ADDSDatabase / Restore-ADDSDatabase use Invoke-Expression with password on command line | High | ADDSOracleModule.psm1:42,60 |
| Singleton Oracle connection shared across all operations — not thread-safe | Medium | OracleConnection.cs:23 |

---

## 12. Standard Drawing Layers

From `csharp/AutoCAD/LayerManager.cs` (original):

| Layer | Color index | Color |
|---|---|---|
| PIPE-STD | 7 | white |
| PIPE-INSULATED | 5 | blue |
| VESSEL | 3 | green |
| INSTRUMENT | 4 | cyan |
| STRUCTURAL | 6 | magenta |
| ELECTRICAL | 2 | yellow |
| ADDS-ANNOTATION | 1 | red |
| ADDS-DIMENSION | 8 | dark grey |

---

## 13. Modernization State (main branch)

### Applied changes

| # | Change | Status |
|---|---|---|
| 1 | Unmanaged ODP.NET 11.2 → managed 19c | Applied |
| 2 | Parameterize all C# Oracle queries | Applied |
| 3 | COM/ActiveX → .NET AutoCAD API | Applied |
| 4 | WinForms → WPF PaletteSet MVVM | Applied |
| 5 | BackgroundWorker → async/await | Applied |
| 6 | Externalize credentials | Applied (partial — adds.config still has plaintext) |
| 7 | Structured logging | Applied |
| 8 | Extract BlockLibraryManager | Applied |
| 9–14 | Various other improvements | Applied |

### Compilation blockers (modernized branch)

1. **No `.csproj`** — project will not build without `Oracle.ManagedDataAccess.Core` NuGet reference
2. **`MainPaletteView` XAML missing** — `InitializeComponent()` in `MainForm.cs:116` will fail at runtime
3. **No `IExtensionApplication`** — no plugin entry point; `OracleConnectionFactory.Configure()` is never called
4. **`ADDSOracleModule.psm1` deleted** with no replacement — backup/restore/admin capability lost

### Evaluator verdict

14 changes applied; ALARMv3 evaluator: 12 flagged, 2 rejected, 0 clean.

---

## 14. Vector Search Quality Note

The vector index (72 chunks, `nomic-embed-text`, 768d) is built from the **modernized** source files using function-level symbol slicing. Known limitations:

- **File headers are not embedded**: each chunk starts at a symbol's first line (`start_line` from the `symbol` table). Lines before the first symbol — which typically contain the most descriptive file-level comments — are dropped.
- **Domain queries return code bodies**: searching "what is ADDS used for" returns function implementations, not descriptive text.
- **Chunks are from modernized code**, not original: ALARMv3-added functions appear as if they were original.

To improve: rebuild the index from the original commit (`cf22e8d`) and include a file-header chunk (lines 1 to `min(first_symbol_start-1, 20)`) for each file in addition to symbol-level chunks.

---

## 15. Querying the Codebase

```bash
/workspaces/ALARMv3/.venv/bin/python3 - <<'EOF'
import sys, sqlite3
sys.path.insert(0, '/workspaces/ALARMv3/.venv/lib/python3.12/site-packages')

DB = '/workspaces/ADDS_ALARMv3/.alarmv3/sessions/32552d53-4281-4c3c-a922-4dd1ad53be8c/analysis.db'

# Direct SQL query against the chunk + vector tables
conn = sqlite3.connect(DB)
rows = conn.execute(
    "SELECT file_path, symbol_name, content FROM code_chunk ORDER BY RANDOM() LIMIT 5"
).fetchall()
for fp, sym, content in rows:
    print(fp.split('/')[-1], sym)
    print(content[:200])
    print('---')
conn.close()
EOF
```

For semantic (vector) queries, Ollama must be running (`nohup ollama serve > /tmp/ollama.log 2>&1 &`). Vector search uses `nomic-embed-text` via the KnowledgeBuilder in `/workspaces/ALARMv3/src/alarmv3/core/knowledge.py`.
