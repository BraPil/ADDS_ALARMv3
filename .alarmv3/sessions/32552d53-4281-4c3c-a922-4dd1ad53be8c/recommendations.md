# ALARMv3 Modernization Recommendations

**Session**: `32552d53-4281-4c3c-a922-4dd1ad53be8c`  
**Source**: `/workspaces/ADDS`

**Total recommendations**: 14

---

## 1. 🔴 Replace unmanaged ODP.NET 11g driver with managed ODP.NET 19c
**Category**: `security` | **Severity**: `critical` | **Effort**: `M`

The codebase targets Oracle ODP.NET unmanaged 11g, which is end-of-life, has known CVEs, and lacks TLS 1.2/1.3 support for Oracle 19c connections. Replace with the fully managed Oracle.ManagedDataAccess.Core NuGet package (ODP.NET 19c+), which supports modern TLS, connection pooling improvements, and requires no native DLL distribution. All connection and stored-procedure code in OracleConnection.cs and StoredProcedures.cs must be re-targeted to the new namespace (Oracle.ManagedDataAccess.Client).

> Running an EOL database driver against Oracle 19c is the single largest security and compatibility risk in the codebase and blocks all other data-layer modernization.

**Affected files**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## 2. 🔴 Migrate project from .NET Framework 4.5 to .NET 10
**Category**: `dependency` | **Severity**: `critical` | **Effort**: `L`

.NET Framework 4.5 reached end-of-support in January 2016 and receives no security patches. Migrating to .NET 10 (LTS) unlocks performance improvements, modern C# language features, and is a prerequisite for using managed ODP.NET Core and the latest AutoCAD .NET API. Use the .NET Upgrade Assistant to convert project files to SDK-style csproj, then resolve breaking-change incompatibilities iteratively.

> An unsupported runtime invalidates any security posture and is incompatible with the stated .NET 10 target architecture.

**Affected files**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`
- `/workspaces/ADDS/csharp/Forms/MainForm.cs`

---

## 3. 🟠 Audit and parameterize all Oracle data-access calls to prevent SQL injection
**Category**: `security` | **Severity**: `high` | **Effort**: `M`

OracleConnection.cs exposes DeleteEquipment and other mutation methods, and StoredProcedures.cs contains a BulkDataLoader class — patterns that commonly concatenate user input into SQL strings in legacy .NET/Oracle codebases. All dynamic SQL construction must be replaced with OracleParameter-bound parameterized queries or confirmed stored-procedure calls. Introduce an integration test suite to validate parameter binding before migration.

> SQL injection via legacy unparameterized Oracle calls is a P0 vulnerability in any industrial design/asset database system.

**Affected files**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## 4. 🟠 Externalize Oracle credentials from connection strings in source files
**Category**: `security` | **Severity**: `high` | **Effort**: `S`

Legacy ODP.NET codebases routinely embed database usernames and passwords directly in source or config files. OracleConnection.cs almost certainly contains hard-coded or config-file-sourced credentials. Migrate all secrets to environment variables or a secrets manager (e.g., Azure Key Vault, Windows DPAPI vault) and reference them at runtime. Rotate any credentials that have existed in source control.

> Credentials in source code or plain config files are a common cause of credential leakage, especially when the repo is shared for modernization review.

**Affected files**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `powershell/admin/ADDSOracleModule.psm1`

---

## 5. 🟠 Replace AutoLISP COM automation layer with AutoCAD .NET API
**Category**: `modernization` | **Severity**: `high` | **Effort**: `XL`

Six LISP files (draw-commands.lsp, pipe-routing.lsp, equipment.lsp, dialog-utils.lsp, etc.) implement the primary drawing commands via the AutoLISP COM bridge, which is deprecated in current AutoCAD releases and unavailable in AutoCAD for Mac/Web. These commands should be reimplemented as AutoCAD .NET API commands (CommandMethod attributes in DrawingManager.cs / LayerManager.cs), enabling type safety, debugging, and access to the full ObjectARX object model.

> AutoLISP COM interop is the primary architectural risk blocking the AutoCAD latest target and is the largest source of unmaintainable code in the system.

**Affected files**:
- `lisp/drawing/draw-commands.lsp`
- `lisp/drawing/pipe-routing.lsp`
- `lisp/drawing/equipment.lsp`
- `lisp/dialogs/dialog-utils.lsp`
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## 6. 🟠 Replace WinForms MainForm with modern AutoCAD palette or WPF panel
**Category**: `modernization` | **Severity**: `high` | **Effort**: `M`

MainForm.cs (80 LOC) uses WinForms, which is architecturally misaligned with AutoCAD's modern dockable palette API and does not render correctly with high-DPI displays common in CAD workstations. Replace the form with an AutoCAD PaletteSet hosting a WPF UserControl (MVVM), which is the Autodesk-recommended pattern for .NET-based AutoCAD plugins. This also eliminates a WinForms dependency from the .NET 10 migration.

> WinForms AutoCAD modal dialogs cause focus and rendering issues in newer AutoCAD versions and block high-DPI support.

**Affected files**:
- `/workspaces/ADDS/csharp/Forms/MainForm.cs`

---

## 7. 🟠 Introduce async/await for all database and sync operations
**Category**: `modernization` | **Severity**: `high` | **Effort**: `M`

SyncService.cs (DoSync, GenerateEquipmentReport, GeneratePipeReport) and OracleConnection.cs (CloseConnection, DeleteEquipment) perform synchronous blocking I/O on what is likely the AutoCAD main thread, causing UI freezes during database operations. Migrate all data-access methods to async/await patterns using OracleCommand.ExecuteReaderAsync and cancellation tokens. This is natively supported in ODP.NET Managed Core.

> Blocking database calls on the AutoCAD main thread cause application hangs that are the top user-reported issue in legacy CAD plugin codebases.

**Affected files**:
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## 8. 🟠 Introduce repository pattern and separate data access from business logic
**Category**: `quality` | **Severity**: `high` | **Effort**: `M`

OracleConnection.cs conflates raw connection management with domain logic (EquipmentRepository class is inside the same file as CloseConnection), and StoredProcedures.cs mixes procedure invocation with BulkDataLoader logic. Extract distinct interfaces (IEquipmentRepository, IPipeRepository) with concrete Oracle implementations, and inject them into SyncService and ProjectService via constructor injection. This enables unit testing without a live Oracle instance.

> Without a clear repository boundary, migrating the Oracle driver is a risky find-and-replace across business logic rather than a single-layer swap.

**Affected files**:
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`

---

## 9. 🟡 Add structured logging to replace assumed Console/Debug trace calls
**Category**: `quality` | **Severity**: `medium` | **Effort**: `S`

A codebase of this vintage targeting AutoCAD plugins typically uses Debug.WriteLine or Console output for diagnostics, which are invisible at runtime. Introduce Microsoft.Extensions.Logging with a file-sink (e.g., Serilog to a rolling log file in %APPDATA%) wired through DI. Log all Oracle connection events, sync operations, and AutoCAD command invocations at appropriate levels to support field diagnostics.

> Without structured logging, diagnosing Oracle connectivity and sync failures in production CAD environments requires reproducing issues locally, which is often impossible.

**Affected files**:
- `/workspaces/ADDS/csharp/Services/SyncService.cs`
- `/workspaces/ADDS/csharp/DataAccess/OracleConnection.cs`
- `/workspaces/ADDS/csharp/Services/ProjectService.cs`

---

## 10. 🟡 Wrap AutoCAD transaction management in a reusable helper to prevent document locks
**Category**: `quality` | **Severity**: `medium` | **Effort**: `S`

DrawingManager.cs (DrawLine, DrawCircle, AddText) and LayerManager.cs (FreezeNonADDSLayers) each open AutoCAD transactions independently, which creates risk of unclosed transactions and database lock leaks if an exception is thrown. Introduce a TransactionHelper class that wraps StartTransaction in a using-compatible scope with guaranteed Commit/Abort, following the AutoCAD .NET API best practice pattern.

> Leaked AutoCAD transactions corrupt the drawing database and are the most frequent cause of crashes in legacy .NET AutoCAD plugins.

**Affected files**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## 11. 🟡 Pin and document all AutoCAD .NET API assembly references by version
**Category**: `dependency` | **Severity**: `medium` | **Effort**: `S`

The AutoCAD .NET API assemblies (acdbmgd.dll, acmgd.dll, etc.) are version-coupled to specific AutoCAD releases and must not be deployed with the plugin. Ensure all AutoCAD references use CopyLocal=false, are pinned to the target AutoCAD version in the project file, and are documented in a compatibility matrix. Add a runtime version-check on plugin load to fail fast with a clear message on version mismatch.

> Mismatched AutoCAD API assemblies cause silent runtime failures or crashes with no actionable error messages for end users.

**Affected files**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`
- `/workspaces/ADDS/csharp/AutoCAD/LayerManager.cs`

---

## 12. 🟡 Migrate PowerShell scripts to enforce strict mode and use approved verbs
**Category**: `quality` | **Severity**: `medium` | **Effort**: `S`

ADDSOracleModule.psm1 and deploy-adds.ps1 use inferred function names (Connect-ADDSOracle, Backup-ADDSDatabase, Deploy-ADDSFiles) that may lack Set-StrictMode, error handling (-ErrorAction Stop), and PSScriptAnalyzer compliance. Add Set-StrictMode -Version Latest, convert all try/catch blocks to use $Error[0] correctly, and run PSScriptAnalyzer in CI to enforce approved verb usage and avoid suppressed errors.

> PowerShell scripts without strict mode silently swallow errors during Oracle backup and deployment, making failures invisible until data loss occurs.

**Affected files**:
- `powershell/admin/ADDSOracleModule.psm1`
- `powershell/deploy/deploy-adds.ps1`

---

## 13. 🟡 Replace BulkDataLoader batch approach with ODP.NET array binding
**Category**: `modernization` | **Severity**: `medium` | **Effort**: `M`

BulkDataLoader in StoredProcedures.cs likely implements row-by-row or manual batching to insert bulk equipment/pipe data into Oracle. ODP.NET Managed Core supports array binding (OracleParameter with value arrays), which sends bulk data in a single round-trip and achieves 10-100x throughput improvement for large datasets. Refactor BulkDataLoader to use OracleCommand array binding with configurable batch sizes.

> Bulk insert performance directly impacts sync times visible to engineers working in AutoCAD, and upgrading the driver makes this optimization available at no additional cost.

**Affected files**:
- `/workspaces/ADDS/csharp/DataAccess/StoredProcedures.cs`

---

## 14. 🟡 Extract BlockLibraryManager from DrawingManager into its own class file
**Category**: `quality` | **Severity**: `medium` | **Effort**: `S`

BlockLibraryManager is defined within DrawingManager.cs (100 LOC total), violating the single-responsibility principle and making both classes harder to test and extend. Extract BlockLibraryManager to its own file (AutoCAD/BlockLibraryManager.cs) with a corresponding IBlockLibraryManager interface. This is a low-risk, high-readability refactor that reduces DrawingManager.cs to a focused drawing-primitive class.

> This is the quickest structural improvement that immediately improves navigability and sets the pattern for subsequent single-responsibility refactors.

**Affected files**:
- `/workspaces/ADDS/csharp/AutoCAD/DrawingManager.cs`

---
