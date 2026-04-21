# Learned Language Grammars (Phase 7)

These grammar descriptors were inferred by ALARMv3 at runtime by sampling ADDS source files and asking Claude to describe the syntax. They are persisted to `memory.db` so future sessions skip the Claude inference step.

---

## `.ps1` — PowerShell

**Notes**: PowerShell is a Microsoft scripting language with a Verb-Noun function naming convention, case-insensitive keywords, and uses Import-Module for dependency loading; functions are defined with the 'function' keyword and support pipeline-oriented parameter binding.

**Function patterns**:
- `(?i)^\s*function\s+([A-Za-z0-9_-]+)\s*[({]?`
- `(?i)^\s*function\s+([A-Za-z0-9_-]+)\s*\(`
- `(?i)^\s*filter\s+([A-Za-z0-9_-]+)\s*[({]?`
**Class/struct patterns**:
- `(?i)^\s*class\s+([A-Za-z0-9_]+)\s*(?::\s*[A-Za-z0-9_]+)?\s*\{?`
**Import patterns**:
- `(?i)^\s*Import-Module\s+['"]?([^'"
]+?)['"]?\s*$`
- `(?i)^\s*using\s+module\s+([A-Za-z0-9_.\\/-]+)`
- `(?i)^\s*using\s+namespace\s+([A-Za-z0-9_.]+)`
- `(?i)^\s*#requires\s+-module\s+([A-Za-z0-9_.,\s]+)`
- `(?i)^\s*\[System\.Reflection\.Assembly\]::Load(?:File|From)?\(['"]([^'"]+)['"]\)`
- `(?i)^\s*Add-Type\s+-(?:Path|AssemblyName)\s+['"]?([^'"
]+?)['"]?\s*$`

---

## `.dcl` — AutoCAD DCL (Dialog Control Language)

**Notes**: AutoCAD DCL is a declarative UI description language used with AutoLISP/Visual LISP; it defines dialog boxes using named tile prototypes with colon-prefixed type annotations and curly-brace blocks, with no functions or imports.

**Class/struct patterns**:
- `^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*dialog\s*\{`
- `^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:\s*(row|column|button|edit_box|list_box|popup_list|image|text|toggle|radio_button|radio_column|radio_row|boxed_column|boxed_row|boxed_radio_column|boxed_radio_row|slider|image_button|errtile|spacer|concatenation)\s*\{`

---

## `.lsp` — AutoLISP

**Notes**: AutoLISP is a Lisp dialect embedded in AutoCAD, using S-expression syntax where functions are defined with defun (including C: prefix for AutoCAD commands) and global variables are conventionally wrapped in asterisks.

**Function patterns**:
- `\(defun\s+([A-Za-z0-9:_*\-]+)\s*[(/]`
- `\(defun-q\s+([A-Za-z0-9:_*\-]+)\s*[(/]`
**Import patterns**:
- `\(load\s+["']?([A-Za-z0-9_.\-/\\]+)["']?`
- `\(load_dialog\s+"([^"]+)"`
- `\(xload\s+["']?([A-Za-z0-9_.\-/\\]+)["']?`

---

## `.config` — Key-Value Configuration File

**Notes**: Simple flat key-value configuration format using '=' as delimiter with no functions, classes, or imports; values may contain paths, special characters, and version strings.


---

## `.psm1` — PowerShell Module

**Notes**: PowerShell Module files (.psm1) are part of the PowerShell scripting language family; functions are defined with the 'function' keyword and names commonly use Verb-Noun conventions with hyphens.

**Function patterns**:
- `(?i)^\s*function\s+([A-Za-z0-9_\-]+)\s*(?:\{|\()`
**Class/struct patterns**:
- `(?i)^\s*class\s+([A-Za-z0-9_]+)`
**Import patterns**:
- `(?i)^\s*(?:Import-Module|using\s+module|require)\s+['"]?([A-Za-z0-9_.\-\/\\]+)['"]?`
- `(?i)^\s*Add-Type\s+.*['"]([A-Za-z0-9_.\-\/\\]+\.dll)['"]`
- `(?i)^\s*using\s+(?:namespace|assembly)\s+([A-Za-z0-9_.]+)`

---
