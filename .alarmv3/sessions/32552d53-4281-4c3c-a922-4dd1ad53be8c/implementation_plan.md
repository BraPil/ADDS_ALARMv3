# ADDS Implementation Plan

**Session**: `32552d53-4281-4c3c-a922-4dd1ad53be8c`  
**Items**: 28

---

Execution order is dependency-resolved: items whose affected files overlap are serialised; independent items run concurrently via `implement_batch`.

---

## Step 1 — 🔴 Migrate project from .NET Framework 4.5 to .NET 10
**Recommendation rank**: 2 | **Category**: `dependency` | **Effort**: `L` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`
- `/workspaces/ADDS/csharp/Forms/MainForm.cs`

---

## Step 1 — 🔴 Migrate project from .NET Framework 4.5 to .NET 10
**Recommendation rank**: 2 | **Category**: `dependency` | **Effort**: `L` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`
- `/workspaces/ADDS/csharp/Forms/MainForm.cs`

---

## Step 2 — 🟠 Replace AutoLISP COM automation layer with AutoCAD .NET API
**Recommendation rank**: 5 | **Category**: `modernization` | **Effort**: `XL` | **Status**: `pending`

**Files in scope**:
- `lisp/drawing/draw-commands.lsp`
- `lisp/drawing/pipe-routing.lsp`
- `lisp/drawing/equipment.lsp`
- `lisp/dialogs/dialog-utils.lsp`
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## Step 2 — 🟠 Replace AutoLISP COM automation layer with AutoCAD .NET API
**Recommendation rank**: 5 | **Category**: `modernization` | **Effort**: `XL` | **Status**: `pending`

**Files in scope**:
- `lisp/drawing/draw-commands.lsp`
- `lisp/drawing/pipe-routing.lsp`
- `lisp/drawing/equipment.lsp`
- `lisp/dialogs/dialog-utils.lsp`
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## Step 3 — 🟠 Introduce repository pattern and separate data access from business logic
**Recommendation rank**: 8 | **Category**: `quality` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`

---

## Step 3 — 🟠 Introduce repository pattern and separate data access from business logic
**Recommendation rank**: 8 | **Category**: `quality` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`

---

## Step 4 — 🟠 Introduce async/await for all database and sync operations
**Recommendation rank**: 7 | **Category**: `modernization` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 4 — 🟠 Introduce async/await for all database and sync operations
**Recommendation rank**: 7 | **Category**: `modernization` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 5 — 🟡 Add structured logging to replace assumed Console/Debug trace calls
**Recommendation rank**: 9 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`

---

## Step 5 — 🟡 Add structured logging to replace assumed Console/Debug trace calls
**Recommendation rank**: 9 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`

---

## Step 6 — 🔴 Replace unmanaged ODP.NET 11g driver with managed ODP.NET 19c
**Recommendation rank**: 1 | **Category**: `security` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 6 — 🔴 Replace unmanaged ODP.NET 11g driver with managed ODP.NET 19c
**Recommendation rank**: 1 | **Category**: `security` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 7 — 🟠 Audit and parameterize all Oracle data-access calls to prevent SQL injection
**Recommendation rank**: 3 | **Category**: `security` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 7 — 🟠 Audit and parameterize all Oracle data-access calls to prevent SQL injection
**Recommendation rank**: 3 | **Category**: `security` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 8 — 🟠 Externalize Oracle credentials from connection strings in source files
**Recommendation rank**: 4 | **Category**: `security` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `powershell/admin/ADDSOracleModule.psm1`

---

## Step 8 — 🟠 Externalize Oracle credentials from connection strings in source files
**Recommendation rank**: 4 | **Category**: `security` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `powershell/admin/ADDSOracleModule.psm1`

---

## Step 9 — 🟡 Wrap AutoCAD transaction management in a reusable helper to prevent document locks
**Recommendation rank**: 10 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## Step 9 — 🟡 Wrap AutoCAD transaction management in a reusable helper to prevent document locks
**Recommendation rank**: 10 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## Step 10 — 🟡 Pin and document all AutoCAD .NET API assembly references by version
**Recommendation rank**: 11 | **Category**: `dependency` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## Step 10 — 🟡 Pin and document all AutoCAD .NET API assembly references by version
**Recommendation rank**: 11 | **Category**: `dependency` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## Step 11 — 🟡 Migrate PowerShell scripts to enforce strict mode and use approved verbs
**Recommendation rank**: 12 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `powershell/admin/ADDSOracleModule.psm1`
- `powershell/deploy/deploy-adds.ps1`

---

## Step 11 — 🟡 Migrate PowerShell scripts to enforce strict mode and use approved verbs
**Recommendation rank**: 12 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `powershell/admin/ADDSOracleModule.psm1`
- `powershell/deploy/deploy-adds.ps1`

---

## Step 12 — 🟠 Replace WinForms MainForm with modern AutoCAD palette or WPF panel
**Recommendation rank**: 6 | **Category**: `modernization` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/Forms/MainForm.cs`

---

## Step 12 — 🟠 Replace WinForms MainForm with modern AutoCAD palette or WPF panel
**Recommendation rank**: 6 | **Category**: `modernization` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/Forms/MainForm.cs`

---

## Step 13 — 🟡 Replace BulkDataLoader batch approach with ODP.NET array binding
**Recommendation rank**: 13 | **Category**: `modernization` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 13 — 🟡 Replace BulkDataLoader batch approach with ODP.NET array binding
**Recommendation rank**: 13 | **Category**: `modernization` | **Effort**: `M` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## Step 14 — 🟡 Extract BlockLibraryManager from DrawingManager into its own class file
**Recommendation rank**: 14 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`

---

## Step 14 — 🟡 Extract BlockLibraryManager from DrawingManager into its own class file
**Recommendation rank**: 14 | **Category**: `quality` | **Effort**: `S` | **Status**: `pending`

**Files in scope**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`

---
