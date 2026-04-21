# ADDS Implementation — Applied Changes

**Session**: `32552d53-4281-4c3c-a922-4dd1ad53be8c`  
**Branch**: `modernized` in BraPil/ADDS_ALARMv3

---

All 14 changes were applied and committed to the `modernized` branch. Each commit corresponds to one accepted recommendation.

---

## Rank 2: Migrate project from .NET Framework 4.5 to .NET 10
**Change ID**: 1 | **Evaluator verdict**: `flag` | **Commit**: `8f279973fc5e2a9c2d7cba4ebad2c53043982b07`

> **Evaluator notes**: Several significant issues found:

1. **Incomplete diff — AutoCAD files and MainForm.cs not touched**: The recommendation explicitly lists DrawingManager.cs, LayerManager.cs, and MainForm.cs as affected files. The diff makes zero changes to these files. DrawingManager.cs and LayerManager.cs still use COM/ActiveX Interop (Autodesk.AutoCAD.Interop) and have no path to .NET 10 without migrating to the AutoCAD .NET API (AcMgd.dll/AcCoreMgd.dll). MainForm.cs still contains a raw SQL injection vector in txtSearch_TextChanged ('SELECT * FROM EQUIPMENT WHERE TAG LIKE \'%' + filter + '%\'') that the diff never fixes. The recommendation is only partially implemented.

2. **ProjectService.cs diff is truncated/incomplete**: The diff for ProjectService.cs adds 'using Oracle.ManagedDataAccess.Client;' but the body of the file still references 'Oracle.DataAccess.Client.OracleCommand' explicitly (in OpenProject) and uses raw string interpolation SQL injection in both OpenProject and CreateProject. These are not corrected.

3. **Shared/singleton OracleConnection is still fundamentally broken for async/multi-threaded use**: The lock makes GetConnection() and CloseConnection() individually thread-safe, but sharing a single OracleConnection across concurrent callers (now made worse because SyncService uses Task.Run) is not safe. OracleConnection is not thread-safe for concurrent command execution. The diff adds a lock but does not fix the underlying design problem, and may give a false sense of safety.

4. **BulkDataLoader.LoadFromFile — tableName SQL injection not resolved**: The diff notes that 'tableName must be validated by the caller against an allowlist' but does not enforce this. The INSERT still uses string interpolation for tableName ($"INSERT INTO {tableName} VALUES (:lineVal)"). No allowlist validation is added anywhere in the diff. The comment is aspirational, not a fix.

5. **BulkDataLoader.GetRowCount — not touched**: This method still uses $"SELECT COUNT(*) FROM {tableName}" with no parameterization and no allowlist check — a SQL injection risk left completely unaddressed by the diff.

6. **StoredProcedureRunner.RunQuery still accepts raw SQL passthrough**: The comment '// Allows raw SQL passthrough - unsafe' was removed but the security issue was not fixed. The method signature and behavior are identical. Callers like MainForm.txtSearch_TextChanged still inject user input directly.

7. **SyncService race condition on _cts**: In StartSync(), _cts is reassigned without disposing the old one if called in rapid succession. If StartSync is called while a previous cancellation is pending but _syncTask.IsCompleted is false, the old CancellationTokenSource leaks.

8. **DoSyncAsync is not actually async**: The method signature is 'async Task' but contains no 'await' expressions, meaning it runs synchronously on the thread pool thread and the compiler will warn about this. It should either use await for I/O calls or not be declared async.

9. **EventLog.WriteEntry removed without equivalent**: SyncComplete previously wrote to Windows EventLog on error. The replacement uses Trace.TraceError which is much weaker for a production service. This is a behavioral regression in observability.

10. **No .csproj changes included**: The recommendation specifically calls out migrating to SDK-style csproj and updating the target framework. No project file changes are in the diff, so the namespace/assembly changes (Oracle.DataAccess → Oracle.ManagedDataAccess.Core NuGet package) have no backing package reference. The code will not compile without the corresponding csproj changes.

**Diff**: +95 / -82 lines

---

## Rank 5: Replace AutoLISP COM automation layer with AutoCAD .NET API
**Change ID**: 3 | **Evaluator verdict**: `reject` | **Commit**: `4113059b8a8c1e70a34efeb691332cd6ef8f8052`

> **Evaluator notes**: The diff is critically incomplete and misleading. The stated recommendation is to 'reimplement commands as AutoCAD .NET API commands (CommandMethod attributes in DrawingManager.cs / LayerManager.cs)' but the diff contains NO changes to DrawingManager.cs or LayerManager.cs whatsoever. Both C# files still use the deprecated COM/ActiveX Autodesk.AutoCAD.Interop layer that the recommendation explicitly says to replace. The LISP files are gutted (commands deleted), but the replacement .NET API implementations are entirely absent.

Specific problems:

1. **Core work not done**: DrawingManager.cs still uses `Marshal.GetActiveObject`, `AcadApplication` COM interop, `AcadDocument`, `AcadLayer`, and `Autodesk.AutoCAD.Interop` — the exact APIs the recommendation says to replace with `[CommandMethod]` attributes and the AutoCAD .NET managed API (AcMgd.dll/AcCoreMgd.dll).

2. **No CommandMethod implementations**: Not a single `[CommandMethod("ADDS-DRAW-PIPE")]` or equivalent .NET command handler is added. Users who type these commands in AutoCAD will get 'Unknown command' errors immediately after this change.

3. **Functional regression — complete feature loss**: Commands deleted from LISP: ADDS-DRAW-PIPE, ADDS-DRAW-VESSEL, ADDS-DRAW-INSTRUMENT, ADDS-ROUTE-PIPE, ADDS-LIST-PIPES, ADDS-EDIT-PIPE, ADDS-PLACE-PUMP, ADDS-PLACE-HEAT-EXCHANGER, ADDS-PLACE-TANK, ADDS-EQUIPMENT-REPORT, ADDS-DELETE-EQUIPMENT. None of these exist anywhere in the post-diff codebase.

4. **dialog-utils.lsp not touched**: The diff description lists it as affected but dialog-utils.lsp is unchanged. It still dispatches to the deleted LISP C: functions (C:ADDS-DRAW-PIPE, C:ADDS-DRAW-VESSEL, etc.) which no longer exist, causing runtime errors when any menu option is selected via C:ADDS.

5. **Global state dependency broken**: equipment.lsp and pipe-routing.lsp reference `*ADDS-DRAWING-SCALE*`, `*ADDS-ORACLE-HOST*`, `*ADDS-ORACLE-PORT*`, `*ADDS-ORACLE-SID*`, `*ADDS-USER-NAME*` set in draw-commands.lsp's global initialization block, which is now deleted. If any LISP still loads (e.g., dialog-utils.lsp), these globals will be unbound, causing errors.

6. **Diff syntax**: The outer diff is wrapped in a fenced block labeled 'diff' but contains inner '---/+++' unified diff headers without a wrapping diff header, suggesting copy-paste formatting issues. The equipment.lsp hunk also ends without a newline on the last line of the replacement block, which is a minor formatting inconsistency.

7. **No migration path or loading guard**: The comment says 'should not be loaded' but no acad.lsp / acaddoc.lsp load-guard changes are included to actually prevent loading.

In summary: this diff deletes production functionality with zero replacement implementation, would cause immediate complete failure of all ADDS drawing commands, and does not actually implement the stated recommendation.

**Diff**: +12 / -231 lines

---

## Rank 8: Introduce repository pattern and separate data access from business logic
**Change ID**: 6 | **Evaluator verdict**: `flag` | **Commit**: `f2d36e63b71084412a9dc34c86df3fcef5bf7ecb`

> **Evaluator notes**: Several concrete problems found:

1. **Truncated diff – ProjectService.cs is incomplete**: The diff for ProjectService.cs cuts off after the constructor injection declaration (`private readonly IStoredProcedureRunner _runner;` and the comment). The body of the class – including the refactored `OpenProject`, `GetProjectList`, and `CreateProject` methods – is entirely missing. The diff as submitted cannot be applied to produce a complete, compilable file.

2. **OracleEquipmentRepository default constructor breaks DI contract**: A default constructor `public OracleEquipmentRepository() { }` is added 'for backwards compatibility', but it ignores the `_factory` field that was declared just above it. All methods in the class still call `OracleConnectionFactory.GetConnection()` statically, making `_factory` a dead, misleading field. The stated benefit of dependency injection (swap the Oracle driver in one layer) is not achieved because the concrete class hard-codes the static factory call regardless of which constructor is used.

3. **`params object[]` forwarding bug in StoredProcedureRunner façade**: The static façade delegates `RunProc` as:
   `=> OracleStoredProcedureRunner.Default.RunProc(procName, args);`
   Here `args` is already an `object[]`, so it will be passed as a *single* `object` argument (the array itself) rather than spread across the `params` list. The instance method will see one parameter (the array) instead of the original individual arguments, silently breaking all existing callers that go through the static façade.

4. **`IEquipmentRepository` and `IPipeRepository` overlap / redundancy**: `IEquipmentRepository` already declares `GetPipeRoutes()`, yet `IPipeRepository` is introduced with the exact same method signature. Nothing in the diff uses `IPipeRepository`, making it dead code and creating confusion about which interface to inject.

5. **`GenerateReport` moved to obsolete static class but not to instance class**: `GenerateReport` is present only on the obsolete static `StoredProcedureRunner` façade; it is not exposed through `IStoredProcedureRunner` or `OracleStoredProcedureRunner`. Any test or injected consumer that needs report generation has no injectable path.

6. **Resource leaks not fixed**: The diff does not add `using` blocks or `try/finally` around connections, commands, adapters, or readers in any of the refactored methods. Every method still leaks OracleConnection/OracleCommand objects on exception, which was a pre-existing problem the refactor had an opportunity to fix.

7. **ReportService static methods ignore the injected field**: `GenerateEquipmentReport` and `GeneratePipeReport` are still `static` methods but the class now has an instance field `_equipmentRepo`. The static methods create a `new OracleEquipmentRepository()` directly instead of using the injected repository, defeating the purpose of the constructor and guaranteeing the injected dependency is never used.

8. **`StoredProcedureRunner` cannot be both static and inherit-from anything**: The old `StoredProcedureRunner` class was non-static. The diff renames the non-static class to `OracleStoredProcedureRunner` and then redeclares `StoredProcedureRunner` as `public static class`. In C# a static class cannot be marked `[Obsolete]` in a way that still compiles without issues if it references instance members, but more critically existing code that does `new StoredProcedureRunner()` (if any exists) will now fail to compile.

9. **Incomplete migration of ProjectService**: The stated goal is to inject `IStoredProcedureRunner` into `ProjectService`, but since that part of the diff is missing, all SQL-injection vulnerabilities in `OpenProject`, `CreateProject`, and the static `ProjectContext` coupling remain unfixed.

**Diff**: +153 / -37 lines

---

## Rank 7: Introduce async/await for all database and sync operations
**Change ID**: 7 | **Evaluator verdict**: `flag` | **Commit**: `7b56a899a30d55e8bb6d2f53e53f84dfab6ca980`

> **Evaluator notes**: Parse error

**Diff**: +100 / -66 lines

---

## Rank 9: Add structured logging to replace assumed Console/Debug trace calls
**Change ID**: 9 | **Evaluator verdict**: `reject` | **Commit**: `e30a424100403226ea1a31036ed9ae1745f205c3`

> **Evaluator notes**: Multiple critical issues found:

1. **Non-existent API - `LoggerFactory.GetLogger<T>()`**: This static method does not exist in Microsoft.Extensions.Logging. The correct patterns are either `LoggerFactory.Create(...).CreateLogger<T>()` or injecting `ILogger<T>` via DI. The diff uses `LoggerFactory.GetLogger<SyncService>()` (and similarly for every other class), which will not compile. This is a fundamental API misuse that makes the entire diff non-functional.

2. **No ILoggerFactory initialization or configuration**: There is no bootstrapping code shown (no Startup, no composition root, no DI container setup, no Serilog file-sink wiring). The recommendation specifically calls for wiring through DI with a file-sink to %APPDATA%, but the diff provides no such configuration. Static fields initialized at class-load time with a non-existent factory call would throw NullReferenceException or a compile error immediately.

3. **Credential leak via logging**: In `OracleConnectionFactory.GetConnection()`, the log message includes `HOST`, `PORT`, `SID`, and `USER` — but the connection string also contains the plaintext password `PASS`. While `PASS` is not directly logged, the log line is one small mistake away from a credential leak. The diff comment even notes 'plaintext' credentials are a known issue. Adding structured logging that touches these constants increases risk surface.

4. **.NET Framework 4.5 incompatibility**: The file headers explicitly state `.NET Framework 4.5`. `Microsoft.Extensions.Logging` targets .NET Standard 2.0+ and its NuGet packages have complex compatibility requirements on .NET 4.5. Serilog integration and the full DI pipeline are not straightforwardly usable on .NET 4.5 without careful version pinning (e.g., MEL 1.x), none of which is addressed.

5. **Truncated diff for ProjectService.cs**: The diff for `ProjectService.cs` is cut off mid-line (`$" VALUES(SYS_GUID(),'{name}','{description}',SYSDATE`), making it syntactically incomplete and inapplicable.

6. **Static logger on instance classes**: `EquipmentRepository`, `InstrumentRepository`, and `ReportService` are instance classes but are given `private static readonly ILogger` fields. This is a minor style issue but inconsistent with proper DI patterns.

7. **No NullReferenceException guard**: If the (broken) logger initialization returns null (rather than failing to compile), every logging call would throw NullReferenceException at runtime, destabilizing the existing EventLog-based error reporting that currently works.

**Diff**: +89 / -2 lines

---

## Rank 1: Replace unmanaged ODP.NET 11g driver with managed ODP.NET 19c
**Change ID**: 11 | **Evaluator verdict**: `flag` | **Commit**: `bde6476759c9d2f8256f85402259b83f05f60d38`

> **Evaluator notes**: The diff correctly performs the stated namespace swap (Oracle.DataAccess → Oracle.ManagedDataAccess) and updates the header comments, so it does implement the minimum literal ask. However, it is dangerously incomplete and introduces no real security improvement while touching security-sensitive code:

1. **Hardcoded plaintext credentials remain untouched** (OracleConnection.cs lines USER/PASS). The recommendation's rationale mentions 'data-layer modernization' but the diff leaves `adds_p@ss_2003!` in source. A reviewer merging this diff may believe the security work is 'done'.

2. **Shared mutable static connection (`_sharedConnection`) is not thread-safe** and is left as-is. ODP.NET managed has different pooling behavior; the singleton pattern may behave differently and cause connection-state races in multi-threaded scenarios.

3. **All SQL injection vulnerabilities are left in place** — string-concatenated queries in SaveEquipment, DeleteEquipment, GetInstrumentsByArea, UpdateInstrument, GenerateReport, BulkDataLoader.LoadFromFile, GetRowCount (tableName injection). The diff makes zero attempt to fix these, yet the recommendation claims to 're-target all connection and stored-procedure code'.

4. **`outputPath` in GenerateReport is user-controlled** with no validation — path traversal risk left untouched.

5. **`BulkDataLoader.LoadFromFile` silently swallows all exceptions** — this is left as-is and could mask data corruption silently.

6. **No .csproj / NuGet reference change is included in the diff.** Without removing the `Oracle.DataAccess` package reference and adding `Oracle.ManagedDataAccess.Core`, the code will not compile. The diff is incomplete as a standalone change.

7. **OCI8 TODO comment removed** without resolving it — the original code noted 'still using OCI8 for some legacy stored proc calls'. That concern is silently deleted rather than addressed.

8. **`Oracle.ManagedDataAccess.Types` namespace** exists but is less commonly needed; confirm all types used (OracleDbType, OracleParameter) are available without it — minor but worth verifying.

In summary: the diff is a cosmetic namespace rename that does not deliver on the security or modernization goals stated in the recommendation, leaves all injection and credential risks intact, and is missing the project-file change required to actually compile.

**Diff**: +6 / -7 lines

---

## Rank 3: Audit and parameterize all Oracle data-access calls to prevent SQL injection
**Change ID**: 14 | **Evaluator verdict**: `flag` | **Commit**: `bd3f7f9225aacd186bf6f9efc680271a4e26fcd2`

> **Evaluator notes**: The diff makes meaningful progress on SQL injection but has several significant remaining issues:

1. **Path traversal in GenerateReport (unaddressed)**: `outputPath` is passed directly to `StreamWriter` without any validation. An attacker can write arbitrary files anywhere on the filesystem. This is a separate P0 vulnerability introduced/visible here but not addressed.

2. **Plaintext credentials still hardcoded**: The diff changes a comment from 'plaintext' to 'plaintext – must be moved to secure store' but leaves the actual password `adds_p@ss_2003!` in the source. The recommendation says to audit security issues; this one is explicitly called out but not fixed.

3. **Shared/singleton connection is not thread-safe**: `_sharedConnection` is a static field with a race condition in `GetConnection()` (double-check lock pattern missing). Multiple threads can simultaneously enter the null check and create multiple connections or use a partially-initialized one. This is a pre-existing bug but the diff doesn't address it even though all the methods now rely on this shared connection.

4. **OracleCommand and OracleDataAdapter not disposed**: None of the `OracleCommand` or `OracleDataAdapter` objects are wrapped in `using` blocks. This causes ODP.NET handle leaks, which can exhaust cursor limits on the Oracle server. The `GenerateReport` fix wraps the reader in `using` correctly, but all other methods still leak commands and adapters.

5. **RunQuery renamed to RunProcQuery but callers are not updated**: The public API change from `RunQuery(string sql)` to `RunProcQuery(string procName, params OracleParameter[])` will break any existing callers at compile time, but the diff shows no updated call sites or deprecation bridge. This is a breaking change.

6. **BulkDataLoader CSV parsing is naive and fragile**: `line.Split(',')` does not handle quoted fields, embedded commas, or escaped characters. A CSV line like `'O'Brien,valve,4"'` will produce wrong field counts, causing ORA-00947 (not enough values) or silently load corrupt data. This may also reintroduce injection if fields contain special Oracle bind characters.

7. **No transaction support in BulkDataLoader**: The bulk load now propagates exceptions (good), but without wrapping the loop in a transaction, a failure midway through the file leaves the table in a partially-loaded state with no rollback.

8. **Parameter name collision risk in RunProc**: The pre-existing `RunProc` method uses `:p0`, `:p1`, etc. as positional bind names AND adds a `:result` output parameter. The new `BulkDataLoader` also uses `:p0`, `:p1`, etc. While these are separate commands, if `RunProc` is ever called alongside bulk operations on a reused connection with cached state it could cause confusion; minor concern but worth noting.

9. **Diff header double-wraps**: The diff is wrapped in triple-backtick diff blocks twice (``` ```diff ... ``` ```), which could cause patch application tools to reject it as malformed.

**Diff**: +89 / -27 lines

---

## Rank 4: Externalize Oracle credentials from connection strings in source files
**Change ID**: 15 | **Evaluator verdict**: `flag` | **Commit**: `4f6f15d6158f14b4a7538a1bec7cb06c6e1e71ee`

> **Evaluator notes**: The diff correctly removes hardcoded credentials and moves to environment variables, which is the right direction. However, several issues remain:

1. **Password still exposed in process argument list (PowerShell)**: The `Start-Process` change for `exp`/`imp` still passes `"$OracleUser/$OraclePass"` as a single concatenated argument string. On Windows, `Start-Process -ArgumentList` converts the array to a command line, and the password remains visible in process listings and audit logs. The claim in the comment that this 'avoids exposing the password in process-list snapshots' is misleading/incorrect. Oracle `exp`/`imp` do support `PARFILE=` or pipes, which would be safer.

2. **Default parameter evaluation timing in PowerShell**: Using `(Get-ADDSEnvVar 'ADDS_DB_HOST')` as a default parameter value means it is evaluated at module load/dot-source time (technically at first call in some PowerShell versions, but still problematic). If the env vars are not set when the module is imported, the module will throw before any function is called, making it impossible to import the module in environments that don't use Oracle at all. This is a regression in usability.

3. **Thread-safety issue in C# (unchanged)**: The `_sharedConnection` singleton pattern has a race condition with no locking, which was present before and not addressed. This is an existing bug but worth noting.

4. **`int.Parse` without validation**: `int.Parse(portStr)` in `BuildConnectionString()` will throw an unguided `FormatException` if `ADDS_DB_PORT` is set to a non-numeric value. `int.TryParse` with a meaningful error message would be safer.

5. **Connection string injection risk**: Values from environment variables (host, sid, user, pass) are interpolated directly into the ODP.NET connection string without any sanitization. A malicious or misconfigured environment variable containing semicolons could alter the connection string structure.

6. **Diff has a duplicate function header**: The diff shows `function Backup-ADDSDatabase {` appearing twice at line 63, suggesting a context/patch error, though this may be an artifact of the diff format.

7. **`Get-ADDSEnvVar` string concatenation bug in PowerShell**: The `throw` statement uses `+` to concatenate strings, which works in PowerShell but produces an array rather than a single string in some edge cases; `-join` or a here-string would be more idiomatic and reliable.

8. **No secret masking**: The password is still stored in a plain `[string]` parameter in PowerShell. Using `[SecureString]` would be more appropriate, though admittedly harder to integrate with legacy `exp`/`imp` CLI tools.

**Diff**: +85 / -24 lines

---

## Rank 10: Wrap AutoCAD transaction management in a reusable helper to prevent document locks
**Change ID**: 18 | **Evaluator verdict**: `flag` | **Commit**: `2a604ab6aea7714d39f58328eb83b1ba954c3d5d`

> **Evaluator notes**: Several issues with this diff:

1. **Recommendation mismatch**: The recommendation asks for a `TransactionHelper` class wrapping `StartTransaction` in a using-compatible scope. The diff instead uses `StartUndoMark`/`EndUndoMark`, which is a different mechanism (undo grouping, not transaction management). While the comments justify this as 'the COM equivalent,' the stated recommendation is not actually implemented — no `TransactionHelper` class exists anywhere in the diff.

2. **`StartUndoMark`/`EndUndoMark` does NOT prevent document locks or database corruption**: These methods group operations for undo purposes only. They do not replace AutoCAD .NET API `Transaction` objects and do not protect against database lock leaks as claimed. The rationale ('leaked AutoCAD transactions corrupt the drawing database') doesn't apply to COM interop, which has no Transaction object to leak. The original code had no transaction leak risk in the first place under COM interop.

3. **Silent exception swallowing in `FreezeNonADDSLayers`**: The inner `catch (COMException)` with no logging silently ignores failures. If a layer fails to freeze for a reason other than 'it is the current layer' (e.g. a locked layer, a COM marshaling error), that failure is silently discarded with no diagnostic information. This is a regression in error visibility.

4. **Nested undo marks risk**: If `DrawLine`, `DrawCircle`, or `AddText` is called from within another undo mark context (e.g. a caller that also wraps in `StartUndoMark`), nested undo marks can cause unexpected undo behavior in AutoCAD. There is no guard against double-marking.

5. **`EndUndoMark` in `finally` after a partial failure**: If `StartUndoMark` itself throws (e.g. no active document), the `finally` block calls `EndUndoMark` without a prior successful `StartUndoMark`, which can corrupt the undo stack in AutoCAD COM. There is no guard for this.

6. **`InsertBlock` is not wrapped**: The diff wraps `DrawLine`, `DrawCircle`, and `AddText` but leaves `InsertBlock` unprotected, creating an inconsistent pattern.

7. **Misleading documentation**: Comments claim this provides 'transaction-safe' behavior equivalent to AutoCAD .NET API transactions, which is factually incorrect. `StartUndoMark` is an undo grouping tool, not a rollback/abort mechanism.

**Diff**: +88 / -11 lines

---

## Rank 11: Pin and document all AutoCAD .NET API assembly references by version
**Change ID**: 19 | **Evaluator verdict**: `flag` | **Commit**: `bedcb7810f9c00031ea29c28320e9889d92a9b65`

> **Evaluator notes**: Several issues found:

1. **Thread-safety race condition on `_checked`**: The `_checked` flag is read and written without any locking mechanism. In a multi-threaded AutoCAD plugin environment (e.g., background threads or parallel initialization), two threads could simultaneously pass the `if (_checked) return;` check, both execute the version validation logic, and one could even see a torn state. A `volatile` keyword or `Interlocked`/`lock` pattern is needed.

2. **Version check logic hardened against wrong assembly**: The check scans already-loaded assemblies for `acmgd`. If `VersionGuard.AssertCompatible()` is called before AutoCAD has loaded `acmgd.dll` into the AppDomain (e.g., during early plugin initialization or in a test harness), it throws an exception saying the assembly is not loaded — which is a false positive. The check should perhaps defer or probe the assembly on disk rather than only scanning loaded assemblies.

3. **`DrawingManager` constructor already hard-codes `"AutoCAD.Application.24"`**: The COM ProgID `"AutoCAD.Application.24"` is itself version-pinned to AutoCAD 2021 (internal version 24). The `VersionGuard` check is therefore partially redundant for `DrawingManager`, but more importantly, if a future developer updates the ProgID without updating `RequiredAcadMajorVersion`, the two guards will be inconsistent with no compile-time linkage between them.

4. **Recommendation calls for project file changes (`CopyLocal=false`) but the diff only touches `.cs` files**: The `.csproj` file is never modified. The stated goal of ensuring `CopyLocal=false` for all AutoCAD references is not implemented — only commented about. This is the most critical part of the recommendation and is entirely missing from the diff.

5. **`VersionGuard` is in `DrawingManager.cs` but referenced from `LayerManager.cs`**: The guard class is defined in `DrawingManager.cs`, not in its own file. If `LayerManager.cs` is compiled in a different assembly or the files are reorganized, this creates a hidden coupling. It also violates single-responsibility for the file.

6. **No `using` statement added for `System.Reflection` in `LayerManager.cs`**: `LayerManager.cs` calls `VersionGuard.AssertCompatible()` which is in the same namespace, so this is fine, but `DrawingManager.cs` now uses `System.Reflection` (for `Assembly`) without verifying the `using System.Reflection;` directive was present before — the diff adds it, which is correct, but it should be verified the diff applies cleanly given the existing file has no such using.

7. **`_checked` static field is not `volatile`**: Even ignoring full thread-safety, without `volatile`, the JIT/CPU may cache the `false` value in a register and never re-read it from memory on certain architectures, meaning the check never fires after the first thread sets it.

8. **Minor**: The compatibility matrix in comments claims `acdbmgd.dll` and `AcCoreMgd.dll` are referenced, but the actual code only uses `Autodesk.AutoCAD.Interop` (COM) assemblies. The runtime check only validates `acmgd`, not `acdbmgd` or `AcCoreMgd`. The comment matrix is misleading documentation for assemblies not actually used in this codebase.

**Diff**: +107 / -2 lines

---

## Rank 12: Migrate PowerShell scripts to enforce strict mode and use approved verbs
**Change ID**: 21 | **Evaluator verdict**: `flag` | **Commit**: `fc73cf8c0d028f5df4b427536f5be1f34726f6e5`

> **Evaluator notes**: Several issues found:

1. **$Error[0] vs $_ in catch blocks**: The diff uses `$err = $Error[0]` inside catch blocks, but the correct idiomatic PowerShell pattern is to use `$_` (the current pipeline object, which IS the ErrorRecord in a catch block). Using `$Error[0]` is technically functional but is slightly less reliable — in nested scenarios or if ErrorActionPreference is involved, `$Error[0]` should match `$_`, but `$_` is the canonical approach and what PSScriptAnalyzer typically expects. This is a minor correctness issue but not a regression.

2. **`Invoke-Expression -ErrorAction Stop` is ineffective**: `Invoke-Expression` does not respect `-ErrorAction Stop` for external process failures (like `exp`, `imp`, `sqlplus`). These are native commands whose exit codes are captured in `$LASTEXITCODE`, not PowerShell terminating errors. The diff adds `-ErrorAction Stop` to `Invoke-Expression $expCmd` and `Invoke-Expression $impCmd`, but if `exp` or `imp` exits with a non-zero code, no exception is thrown — the catch block is never triggered. The backup/restore failures remain silently swallowed. The fix for `sc.exe` in Install-ADDSService correctly checks `$LASTEXITCODE`, but this pattern is inconsistently not applied to Backup-ADDSDatabase, Restore-ADDSDatabase, or Test-OracleConnection.

3. **Security issues left unaddressed**: The recommendation mentions PSScriptAnalyzer compliance, but the diff retains plaintext passwords in default parameter values and in the `Invoke-Expression` command strings (exposing credentials in process lists). These are pre-existing issues, but the diff comments on them without fixing them, leaving a false impression that they've been reviewed.

4. **`Set-StrictMode` placement in .psm1**: Placing `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'` at module scope in a .psm1 file sets these for the entire module scope, which is correct. However, `$ErrorActionPreference = 'Stop'` at module scope can affect callers in unexpected ways if they import this module — the ErrorActionPreference set inside a module scope should be scoped to functions or the module, not leak into caller scope. In practice, module-scope `$ErrorActionPreference` does not propagate to callers in PowerShell, so this is acceptable but worth noting.

5. **Missing `Export-ModuleMember` line**: The diff does not show the `Export-ModuleMember` line from the original file being preserved. Given the diff format only shows changes, it should still be there, but the absence from the diff context is slightly concerning — reviewers cannot confirm it wasn't accidentally dropped.

6. **`Invoke-Expression` still used for injection-prone patterns**: The recommendation implicitly targets unsafe patterns, but the diff explicitly retains all `Invoke-Expression` calls with user-controlled inputs (noted with comments), meaning injection vulnerabilities remain. This is a pre-existing issue not introduced by the diff, but the diff does nothing to address it despite the recommendation mentioning PSScriptAnalyzer compliance (which would flag `Invoke-Expression` usage).

Summary: The most significant functional bug is that `Invoke-Expression -ErrorAction Stop` does not catch external command failures in Backup-ADDSDatabase, Restore-ADDSDatabase, and Test-OracleConnection, meaning the primary stated goal (making backup failures visible) is NOT achieved for those critical functions.

**Diff**: +150 / -74 lines

---

## Rank 6: Replace WinForms MainForm with modern AutoCAD palette or WPF panel
**Change ID**: 23 | **Evaluator verdict**: `flag` | **Commit**: `b3ecdafb4507888db23a009ccc8d79441e8522b5`

> **Evaluator notes**: Several concrete issues found:

1. **Incomplete implementation – XAML missing**: The diff states 'XAML omitted – define in MainPanel.xaml' but provides no such file and no guidance on its content. `InitializeComponent()` is commented out in the code-behind, meaning the UserControl has no UI at all as shipped. This is not a working replacement.

2. **`MainPanel` constructor silently skips `InitializeComponent()`**: The comment says 'If using a .xaml file, InitializeComponent() is generated' but the call is absent. Without either a XAML file or programmatic UI construction, the panel renders blank. This is a regression from the original form.

3. **`MainViewModel` constructor throws unhandled exceptions on startup**: The original code wrapped initialization in a try/catch in `MainForm_Load`. The new constructor calls `LoadEquipmentGrid()` and `new DrawingManager()` with no error handling. Any DB or AutoCAD connection failure will crash palette creation entirely, with no user-visible error.

4. **`ReportForm.ShowDialog()` still called from ViewModel**: `ShowEquipReport()` creates and shows a WinForms `ReportForm` modal dialog directly from the ViewModel. This reintroduces a WinForms dependency (the stated reason for the change), violates MVVM (ViewModel should not own UI), and causes the same focus/rendering issues the recommendation was meant to fix.

5. **`ApplyEquipmentFilter` is not truly parameterised**: The comment claims 'parameterised-style expression; the value is escaped'. However, `DataView.RowFilter` is not a SQL parameter – it is a string expression evaluated by the DataView engine. Replacing `'` with `''` is a partial mitigation but not equivalent to true parameterisation. Special characters such as `[`, `]`, `*`, `%`, and `\` in the DataView expression language can still cause unexpected filter behaviour or errors.

6. **`_paletteSet` and `_vm` are static fields**: If AutoCAD unloads and reloads the plugin, or if `PaletteSetDestroy` fires and `Release()` is called, `_paletteSet` is never set back to `null`. Subsequent calls to `ADDS_OPEN` will attempt `_paletteSet.Visible = true` on a destroyed object, likely throwing an ObjectDisposedException.

7. **`PaletteSetStyles` flag combination syntax**: The diff uses `|` (bitwise OR) to combine `PaletteSetStyles` enum values in an object initializer. While syntactically valid C#, this only works if `PaletteSetStyles` is decorated with `[Flags]`. This is AutoCAD API-specific and should be verified; if not a Flags enum the expression silently sets an invalid value.

8. **`AsyncRelayCommand` does not marshal `LoadEquipmentGrid` back to UI thread**: After `await Task.Run(...)`, `LoadEquipmentGrid()` is called which sets `EquipmentView` (a `DataView`) on the ViewModel. `DataView` modifications from a non-UI thread can cause cross-thread exceptions in WPF bindings. There is no `Dispatcher.InvokeAsync` or `SynchronizationContext` capture.

9. **`DrawPipe` calls `_drawingMgr.SetLayer` from WPF dispatcher thread**: AutoCAD document operations must run on the AutoCAD main thread (via `Document.SendStringToExecute` or `Editor` commands), not the WPF UI thread. The original code had the same issue, but the new code does not address it despite claiming architectural improvement.

10. **Hardcoded GUID**: Using a hardcoded, non-random GUID (`A1B2C3D4-E5F6-7890-ABCD-EF1234567890`) for the PaletteSet identity is a well-known example/placeholder value. If another plugin coincidentally uses the same GUID, AutoCAD will merge or conflict the palettes.

**Diff**: +182 / -54 lines

---

## Rank 13: Replace BulkDataLoader batch approach with ODP.NET array binding
**Change ID**: 26 | **Evaluator verdict**: `flag` | **Commit**: `c13a0a4d85af474ed10e630d44165acbe417913c`

> **Evaluator notes**: Several significant issues with this diff:

1. **Driver version mismatch (critical)**: The file header explicitly states 'ODP.NET unmanaged Oracle.DataAccess 11.2'. The `ArrayBindCount` property and array binding support via `OracleParameter.Value = array` requires ODP.NET Managed Driver (Oracle.ManagedDataAccess) or at minimum a modern unmanaged driver. ODP.NET 11.2 unmanaged may not fully support this pattern reliably, or behavior may differ from what the diff assumes. The recommendation says to use 'ODP.NET Managed Core' but the diff does not change the `using` directives — it still imports `Oracle.DataAccess.Client` (unmanaged), not `Oracle.ManagedDataAccess.Client`.

2. **Column count mismatch not validated**: If a row has fewer columns than the first row established `columnCount`, `batch[row][i]` will throw an `IndexOutOfRangeException` inside `ExecuteBatch`. No guard exists for jagged CSV rows.

3. **Column count mismatch in the other direction**: If a row has *more* columns than `columnCount`, the extra data is silently dropped. This is a regression from the original behavior (which at least attempted to insert the full line).

4. **No transaction wrapping**: Batches are committed independently with no transaction. A failure on batch N leaves batches 0..N-1 committed and N..end not inserted, with no rollback mechanism. The original code had the same problem, but the diff makes it worse by advertising 'improved reliability' implicitly while still having this issue.

5. **Connection management not addressed**: `OracleConnectionFactory.GetConnection()` is called once and the connection is never disposed or closed in the new code either, same as original. The `OracleCommand` objects created in `ExecuteBatch` are also never disposed, which is a resource leak, especially inside a loop.

6. **tableName injection in GetRowCount not fixed**: The diff adds an injection guard for `LoadFromFile`'s `tableName`, but `GetRowCount` still concatenates `tableName` directly into SQL with no validation — an inconsistency that could mislead reviewers into thinking injection is fully addressed.

7. **CSV parsing is naive**: `line.Split(',')` does not handle quoted fields containing commas, which is a regression if the file format contains such data. The original code passed the raw line directly, which at least preserved the caller's intent (however unsafe).

8. **Parameter naming convention**: The diff names parameters `:p{i}` (with colon prefix) in the SQL string AND uses `$":p{i}"` as the parameter name in `OracleParameter`. In ODP.NET, the parameter name should typically be provided *without* the leading colon (i.e., `p0`, not `:p0`). This may cause parameter binding failures at runtime depending on driver version.

9. **batchStartRow tracking is slightly off**: After `batch.Clear()`, `batchStartRow` is set to `fileRowNumber + 1`, which is correct for the *next* batch. However, blank lines are skipped with `continue` before `fileRowNumber` is meaningfully used for the batch boundary, so the reported row numbers in error messages may not accurately reflect actual file positions.

10. **Double-nested diff syntax**: The diff is wrapped in triple-backtick diff blocks twice, suggesting a formatting/tooling error, though this doesn't affect the code itself.

**Diff**: +83 / -9 lines

---

## Rank 14: Extract BlockLibraryManager from DrawingManager into its own class file
**Change ID**: 27 | **Evaluator verdict**: `flag` | **Commit**: `1c0fa73ccae42684011dcfa1c060bb89a43f5e10`

> **Evaluator notes**: The refactor is structurally sound but introduces a breaking API change that callers may not handle: the original BlockLibraryManager had **static** methods (GetAvailableBlocks, LoadBlockLibrary), but the new version converts them to **instance** methods. Any existing callers using BlockLibraryManager.GetAvailableBlocks() or BlockLibraryManager.LoadBlockLibrary(...) as static calls will fail to compile after this change. The diff makes no attempt to find or update such callers.

Additional issues:
1. The IBlockLibraryManager interface accepts a concrete DrawingManager parameter in LoadBlockLibrary(), which couples the interface directly to the concrete class — defeating part of the purpose of the interface abstraction. It should accept IDrawingManager (or similar) if the goal is testability.
2. The hardcoded path C:\ADDS\Blocks\ remains in both versions with no path injection, so the class is still untestable without filesystem state despite gaining an interface.
3. Path concatenation `BLOCK_LIBRARY_PATH + category` without Path.Combine is a latent bug (no separator guaranteed) that existed before and is carried forward unchanged.
4. The interface and the concrete class are in the same file, which is a minor style concern but acceptable for a single file.
5. No null/empty check on `category` in LoadBlockLibrary, also carried forward.

The static-to-instance conversion is the primary concrete risk: it is a silent but real breaking change for any callers outside the reviewed file.

**Diff**: +36 / -21 lines

---
