# ADDS Codebase Wiki
> Generated from direct source-file analysis — 2026-04-21  
> All claims traceable to specific files and line numbers.

---

## 1. What is ADDS?

**ADDS — Automated Drawing & Design System** (name sourced from `lisp/dialogs/main-menu.dcl:5`).

ADDS is an AutoCAD plugin that lets engineers:

1. Place engineering objects (pumps, heat exchangers, tanks, vessels, pipes, instruments) into AutoCAD drawings using interactive commands
2. Persist those objects — with tags, coordinates, types, and metadata — to an Oracle database via two separate interfaces (OADB for AutoLISP, ODP.NET for C#)
3. Track instrument readings (the `INSTRUMENTS` table carries `LAST_VALUE` and `TIMESTAMP` columns — operational data, not just design geometry)
4. Run nightly sync jobs via a Windows Service (`ADDSSyncService`) and a PowerShell batch that exports equipment XML and instrument CSVs to a local cache
5. Manage drawing layers (8 standard layers: PIPE-STD, PIPE-INSULATED, VESSEL, INSTRUMENT, STRUCTURAL, ELECTRICAL, ADDS-ANNOTATION, ADDS-DIMENSION)
6. Load block libraries from `C:\ADDS\Blocks\` by category

**What I can confirm from the code**: a drawing + data management tool for engineering objects with Oracle persistence and a background sync layer.  
**What I cannot confirm from code alone**: the exact domain context (what type of facility, what discipline, what workflow it fits into). The system owner should supply that description.

---

## 2. File Inventory

| File | Lines | Language | Role |
|---|---|---|---|
| `lisp/dialogs/dialog-utils.lsp` | 59 | AutoLISP | Entry point (`C:ADDS`), dialog dispatch, settings dialog |
| `lisp/dialogs/main-menu.dcl` | 88 | DCL | Main menu + settings dialog UI definitions |
| `lisp/drawing/draw-commands.lsp` | 96 | AutoLISP | Draw pipe, vessel, instrument commands; global state init; config loader |
| `lisp/drawing/equipment.lsp` | 105 | AutoLISP | Place pump, HX, tank; equipment report; delete equipment |
| `lisp/drawing/pipe-routing.lsp` | 116 | AutoLISP | Route pipe, list pipes, edit pipe; OADB connect/disconnect helpers; sanitize-tag |
| `lisp/utils/db-utils.lsp` | 80 | AutoLISP | DB init, offline queue (db_queue.sql), flush queue, raw query |
| `lisp/utils/string-utils.lsp` | 55 | AutoLISP | Trim, upper, split, replace, format-tag, validate-tag |
| `csharp/AutoCAD/DrawingManager.cs` | 111 | C# | Draw line/circle/block/text via .NET API; save drawing; get layers |
| `csharp/AutoCAD/BlockLibraryManager.cs` | 50 | C# | Load block library from category path; list available blocks |
| `csharp/AutoCAD/LayerManager.cs` | 139 | C# | Create standard layers; freeze/thaw; purge unused |
| `csharp/DataAccess/OracleConnection.cs` | 134 | C# | `OracleConnectionFactory`, `EquipmentRepository`, `InstrumentRepository` |
| `csharp/DataAccess/StoredProcedures.cs` | 104 | C# | `StoredProcedureRunner`, `BulkDataLoader` (ODP.NET array binding) |
| `csharp/Forms/MainForm.cs` | 129 | C# | `ADDSPaletteCommand`, `MainPaletteViewModel`, `MainPaletteView` (WPF MVVM) |
| `csharp/Services/ProjectService.cs` | 72 | C# | Open/list/create projects in `PROJECTS` table |
| `csharp/Services/SyncService.cs` | 125 | C# | `SyncService` (async loop, 1-min cadence); `ReportService` |
| `powershell/db/sync-oracle.ps1` | 130 | PowerShell | Nightly sync: equipment XML cache + instrument CSV export; health check; alert email |
| `powershell/deploy/deploy-adds.ps1` | 158 | PowerShell | Deploy: prerequisites check, file copy, config update, Oracle test, install Windows Service |
| `sql/schema.sql` | 50 | SQL | DDL for EQUIPMENT, PIPE_ROUTES, INSTRUMENTS, VESSELS; two sequences |
| `config/adds.config` | 9 | Config | Oracle host/port/SID, AutoCAD version, block library path, log path, unit system |
| **Total** | **1,810** | | 20 files, 5 languages |

---

## 3. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  AutoCAD Process                                             │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  AutoLISP Layer                                      │   │
│  │  C:ADDS → DCL menu → C:ADDS-DRAW-*, C:ADDS-PLACE-* │   │
│  │  db-utils: OADB connect/queue/flush                  │   │
│  │  draw-commands: global state, config loader          │   │
│  └─────────────────────────────────────────────────────┘   │
│                │ ads_oadb_* (Oracle 7/8 OADB ADS API)       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  C# Plugin (ADDS.dll)                               │   │
│  │  DrawingManager   — .NET AutoCAD API                │   │
│  │  LayerManager     — layer create/freeze/purge        │   │
│  │  BlockLibraryMgr  — .dwg block loading              │   │
│  │  ADDSPaletteCmd   — WPF PaletteSet (equipment grid) │   │
│  │  EquipmentRepository / InstrumentRepository          │   │
│  │  SyncService      — 1-min async loop                │   │
│  └─────────────────────────────────────────────────────┘   │
│                │ ODP.NET managed (Oracle.ManagedDataAccess)  │
└─────────────────────────────────────────────────────────────┘
                 │
    ┌────────────┴────────────┐
    │    Oracle Database      │
    │  EQUIPMENT              │
    │  PIPE_ROUTES            │
    │  INSTRUMENTS            │
    │  VESSELS                │
    │  PROJECTS (inferred)    │
    │  HEAT_EXCHANGERS (inferred) │
    └────────────────────────┘
                 │
    ┌────────────┴──────────────────────────────┐
    │  Windows Service: ADDSSyncService          │
    │  sync-oracle.ps1 (nightly Task Scheduler)  │
    │  → C:\ADDS\Cache\equipment_<tag>.xml       │
    │  → C:\ADDS\Cache\instruments.csv           │
    └───────────────────────────────────────────┘
```

---

## 4. User Entry Points

### Interactive (AutoCAD command line)

```
Type: ADDS
  → C:ADDS (dialog-utils.lsp:55)
      1. adds-db-init  (connects OADB, sets *ADDS-USER-NAME*)
      2. adds-show-main-menu  (loads main-menu.dcl)
         Menu buttons:
         [Draw Pipe]            → C:ADDS-DRAW-PIPE
         [Draw Vessel]          → C:ADDS-DRAW-VESSEL
         [Place Pump]           → C:ADDS-PLACE-PUMP
         [Place Heat Exchanger] → C:ADDS-PLACE-HEAT-EXCHANGER
         [Route Pipe]           → C:ADDS-ROUTE-PIPE
         [Equipment Report]     → C:ADDS-EQUIPMENT-REPORT
         [Sync Database]        → adds-db-flush-queue
         [Settings]             → adds-show-settings
```

Each `C:ADDS-*` function can also be invoked directly at the AutoCAD command prompt (all are `defun C:*`).

### Background / Scheduled

- `ADDSSyncService` Windows Service: `SyncService.RunSyncLoopAsync()` — every 1 minute, calls `ADDS_PKG.UPDATE_EQUIPMENT_CACHE` stored procedure for each equipment record (`SyncService.cs:50–68`)
- `sync-oracle.ps1`: Windows Task Scheduler, nightly — syncs last-hour equipment changes + all instrument readings to local cache; sends SMTP alert on failure (`sync-oracle.ps1:117–130`)

---

## 5. Oracle Database Schema

From `sql/schema.sql` (DDL dated 1991, last modified 2008):

```sql
EQUIPMENT    (TAG pk, TYPE, MODEL, LOCATION, CREATED_BY, CREATED_DATE, MODIFIED)
PIPE_ROUTES  (ROUTE_ID pk via SEQ_ROUTE, TAG, SPEC, START_PT, END_PT, LENGTH, CREATED_DATE)
INSTRUMENTS  (TAG pk, INSTR_TYPE, AREA, POS_X, POS_Y, LAST_VALUE, TIMESTAMP,
              CREATED_BY, CREATED_DATE, UPDATED)
VESSELS      (TAG pk, CTR_X, CTR_Y, RADIUS, CREATED_BY, CREATED_DATE)
```

Notable issues in the DDL:
- No foreign key constraints anywhere
- No indexes on any columns other than PKs
- `LOCATION` stored as `VARCHAR2(200)` (stringified coordinate pair, not X/Y columns)
- `INSTRUMENTS.LAST_VALUE VARCHAR2(50)` — instrument reading stored as string

**Tables referenced in code but absent from schema.sql:**
- `HEAT_EXCHANGERS` — used in `equipment.lsp:64`
- `PROJECTS` — used in `ProjectService.cs:26`

---

## 6. Global State (AutoLISP)

Defined in `draw-commands.lsp:84–96`. Set at load time, overridden by `adds-load-config`:

```lisp
*ADDS-DRAWING-SCALE*    ; default 1.0
*ADDS-CURRENT-PROJECT*  ; nil until set
*ADDS-DB-CONNECTION*    ; nil until adds-db-init
*ADDS-USER-NAME*        ; "" until DB connect
*ADDS-UNIT-SYSTEM*      ; "IMPERIAL"
*ADDS-LAYER-PREFIX*     ; "ADDS-"
*ADDS-ORACLE-HOST*      ; "ORACLE19C-PROD" (overridden from adds.config)
*ADDS-ORACLE-PORT*      ; 1521
*ADDS-ORACLE-SID*       ; "ADDSDB"
```

Config file (`C:\ADDS\adds.config`) is read by `adds-load-config` at LISP load time via AutoCAD's `open`/`read-line`.

---

## 7. Oracle Connectivity — Two Separate Stacks

ADDS has two independent Oracle connection mechanisms that do not share code:

| Layer | API | Source | Authentication |
|---|---|---|---|
| AutoLISP | `ads_oadb_*` (Oracle 7/8 OADB ADS functions) | pipe-routing.lsp, db-utils.lsp | Reads `ADDS_ORACLE_USER`/`ADDS_ORACLE_PASS` env vars at runtime |
| C# | `Oracle.ManagedDataAccess.Client` (ODP.NET managed 19c) | OracleConnection.cs | `OracleConnectionFactory.Configure()` reads `IConfiguration` or env vars |

The AutoLISP side uses string-concatenated SQL (OADB has no bind variable support); SQL injection mitigation is the `adds-sanitize-tag` whitelist function (`[A-Za-z0-9_-]`).

The C# side uses parameterized `OracleParameter` throughout.

---

## 8. Drawing Commands Reference

### AutoLISP commands (interactive)

| Command | File:Line | What it does |
|---|---|---|
| `C:ADDS` | dialog-utils.lsp:55 | Entry point: DB init + main menu |
| `C:ADDS-DRAW-PIPE` | draw-commands.lsp:4 | Prompts start/end/diameter → draws LINE on PIPE-STD layer → logs event |
| `C:ADDS-DRAW-VESSEL` | draw-commands.lsp:15 | Prompts center/radius/tag → draws CIRCLE on VESSEL layer → saves to DB |
| `C:ADDS-DRAW-INSTRUMENT` | draw-commands.lsp:42 | Prompts location/tag/type → inserts INSTR-{type} block → saves to DB |
| `C:ADDS-PLACE-PUMP` | equipment.lsp:5 | Prompts point/tag/model → inserts PUMP-CENTRIFUGAL block → saves EQUIPMENT |
| `C:ADDS-PLACE-HEAT-EXCHANGER` | equipment.lsp:52 | Prompts point/tag/shell/tube/area → inserts HX-SHELLTUBE → saves HEAT_EXCHANGERS |
| `C:ADDS-PLACE-TANK` | equipment.lsp:70 | Prompts point/tag/capacity/material → draws CIRCLE → saves EQUIPMENT |
| `C:ADDS-ROUTE-PIPE` | pipe-routing.lsp:5 | Prompts start/end → orthogonal 2-segment route → draws LINEs → saves PIPE_ROUTES |
| `C:ADDS-LIST-PIPES` | pipe-routing.lsp:97 | Queries PIPE_ROUTES, prints tag/spec/length to prompt |
| `C:ADDS-EDIT-PIPE` | pipe-routing.lsp:109 | Prompts tag → sets MODIFIED=SYSDATE on PIPE_ROUTES row |
| `C:ADDS-EQUIPMENT-REPORT` | equipment.lsp:83 | Queries EQUIPMENT, prints tag/type/model to prompt |
| `C:ADDS-DELETE-EQUIPMENT` | equipment.lsp:94 | Prompts tag + confirm → deletes from EQUIPMENT |

### C# methods (programmatic)

`DrawingManager`: `DrawLine`, `DrawCircle`, `InsertBlock`, `AddText`, `SaveDrawing`, `SaveDrawingAs`, `GetAllLayerNames`, `ZoomExtents`  
`LayerManager`: `SetupStandardLayers`, `FreezeNonADDSLayers`, `ThawAllLayers`, `PurgeUnusedLayers`, `GetLayersByPrefix`  
`BlockLibraryManager`: `GetAvailableBlocks`, `LoadBlockLibrary`

---

## 9. Offline Write Queue

When `*ADDS-DB-CONNECTION*` is nil (Oracle unreachable), `db-utils.lsp` queues DML as raw SQL strings to `C:\ADDS\db_queue.sql`:

```
adds-db-save-vessel / adds-db-save-instrument
  └── if no connection → adds-db-queue-write → appends INSERT SQL string to file
```

Flushed by `adds-db-flush-queue` ("Sync Database" button): replays each line via `ads_oadb_execute`, deletes file on success.

**Risk**: the queue contains unparameterized SQL. The strings were sanitized when written, but the file is not re-validated at flush time. External modification of `db_queue.sql` would execute arbitrary SQL.

---

## 10. Standard Drawing Layers

From `LayerManager.cs:23–32`:

| Layer | AutoCAD color index |
|---|---|
| PIPE-STD | 7 (white) |
| PIPE-INSULATED | 5 (blue) |
| VESSEL | 3 (green) |
| INSTRUMENT | 4 (cyan) |
| STRUCTURAL | 6 (magenta) |
| ELECTRICAL | 2 (yellow) |
| ADDS-ANNOTATION | 1 (red) |
| ADDS-DIMENSION | 8 (dark grey) |

---

## 11. Security Findings

| Finding | Severity | Location |
|---|---|---|
| Plaintext password in `adds.config` (`ORACLE_PASS=adds_p@ss_2003!`) | Critical | config/adds.config:5 |
| Offline queue replays raw SQL strings without re-validation at flush time | High | db-utils.lsp:51–63 |
| `adds-sanitize-tag` is duplicated in equipment.lsp and pipe-routing.lsp — divergence risk | Medium | equipment.lsp:26, pipe-routing.lsp:74 |
| `BulkDataLoader` uses string interpolation for table name after allowlist check (safe but fragile) | Low | StoredProcedures.cs:79, 87 |

---

## 12. Modernization State (main branch, 2026-04-21)

### Changes applied

| Area | Before | After |
|---|---|---|
| Oracle driver (C#) | `Oracle.DataAccess` unmanaged 11.2 | `Oracle.ManagedDataAccess.Client` ODP.NET 19c |
| AutoCAD API (C#) | COM/ActiveX, `Marshal.GetActiveObject` | Native `AcMgd.dll`/`AcCoreMgd.dll` .NET API |
| UI (C#) | WinForms `MainForm` | WPF `PaletteSet` + MVVM |
| Async (C#) | `BackgroundWorker` | `async/await`, `CancellationTokenSource` |
| Credential handling | Hardcoded strings | Env vars / `IConfiguration` / Windows Credential Manager |
| SQL (C#) | String concatenation | Parameterized `OracleParameter` |
| Logging (C#) | `Console.WriteLine` | `Microsoft.Extensions.Logging.ILogger` |
| Class structure | `BlockLibraryManager` inside `DrawingManager` | Extracted to own file |

### Compilation blockers

1. **No `.csproj` file** — `Oracle.ManagedDataAccess.Core` NuGet reference missing; project will not build.
2. **`MainPaletteView` XAML missing** — `MainForm.cs:116` calls `InitializeComponent()` but no `.xaml` file exists; palette will fail at runtime.
3. **No plugin entry point** — no `IExtensionApplication` implementation; `OracleConnectionFactory.Configure()` is never called; the C# data access layer is uncallable.

### Evaluator verdict summary

14 changes applied. ALARMv3 evaluator: 12 flagged, 2 rejected. No change passed clean.  
The LISP drawing commands (`C:ADDS-*`) are all present in the current files — the note about "complete feature loss" from an earlier analysis was based on a corrupted intermediate state subsequently fixed in the "Fix corrupted modernized source files" commit.

---

## 13. Querying the Codebase (Vector Search)

72 code chunks embedded via `nomic-embed-text` (768 dimensions) in `analysis.db/chunk_vectors`. Ollama must be running (`ollama serve`).

```bash
python3 - <<'EOF'
import sys
sys.path.insert(0, '/workspaces/ALARMv3/src')
from alarmv3.core.knowledge import KnowledgeBuilder
kb = KnowledgeBuilder('/workspaces/ADDS_ALARMv3/.alarmv3/sessions/32552d53-4281-4c3c-a922-4dd1ad53be8c/analysis.db')
results = kb.query("your question here", top_k=5)
for r in results:
    print(r['file_path'], r['symbol_name'])
    print(r['content'][:300])
    print('---')
EOF
```
