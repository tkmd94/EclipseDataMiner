# ESAPI Data Mining Detailed Design Specification (v3.0)

**English** | [日本語](DESIGN_SPECIFICATION.md)

## 1. System Overview
This software is a standalone WPF application designed to rapidly and safely extract DVH metrics and metadata from several thousand to 10,000+ radiation therapy plans (PlanSetup / PlanSum) utilizing the Varian Eclipse Scripting API (ESAPI). Extracted records are streamed into normalized CSV and machine-learning-ready JSON Lines (JSONL) formats to minimize downstream data cleansing efforts.

## 2. Architectural Design
* **Target Platform**: .NET Framework 4.6.1 (Eclipse v16.1 compatible) / `x64` Target
  * Project format: SDK-style csproj / `<LangVersion>10.0</LangVersion>`
* **UI Framework**: Windows Presentation Foundation (WPF)
* **Design Pattern**: MVVM Pattern (`CommunityToolkit.Mvvm`)
* **Project Structure**:
  * Single main project (`EclipseDataMiner`) + unit test project (`EclipseDataMiner.Tests`)
  * Clean layering: Models (DTO), ViewModels, Views, Services, Helpers
* **Decoupled Intermediate Data Model (DTO) Architecture**:
  * ESAPI objects are immediately mapped into independent DTO models (such as `ExtractionPlanRecord`).
  * CSV/JSONL serialization, sanitization, and aggregation logic operate purely on DTOs, enabling 100% offline unit testing without ESAPI connections.
* **Asynchronous Threading Model**:
  * A dedicated Single-Threaded Apartment (STA) worker thread runs all background operations to keep the UI completely responsive.
  * In compliance with ESAPI STA constraints, plan iteration executes strictly sequentially. Concurrent operations such as `Parallel.ForEach` are prohibited.
* **Streaming Data Export Pipeline**:
  * Records are flushed to disk per patient/plan, keeping memory usage flat regardless of total query size.
* **Packaging**:
  * `Costura.Fody` bundles external NuGet dependencies into a single standalone binary (`EclipseDataMiner.exe`), excluding ESAPI DLLs.

## 3. Functional Requirements
### 3.1. Search & Filtering Engine
* **Hierarchical AND / OR Search**:
  * **In-field OR Search**: Comma-separated entries evaluate with OR logic (e.g., PlanID = `VMAT, IMRT`).
  * **Global Logic Switch**: Toggles between AND (all criteria must be satisfied) and OR (any criterion matches).
* **Target Attributes**: Patient ID, Course ID, Plan ID, Dose/Fr, Fractions, Total Dose, Approval Status.
* **Composite PlanSum Support**:
  * Optional `Include PlanSum` checkbox (default: off).
  * Automatically populates non-applicable fields (e.g., beam logs and optimization objectives) with `N/A`.

### 3.2. Structure Pre-Scan & Nomenclature Mapping (Two-Pane Layout with Real-Time Preview)
* **Two-Pane Architecture**:
  * **Left Pane (Mapping Rules)**: Persistent rule dictionary (`Pattern`, `MatchMode`, `TargetAlias`, `IsSelected`). Pre-scanning does not overwrite loaded rules. `[▲ Up]` / `[▼ Down]` buttons control evaluation precedence.
  * **Right Pane (Discovered Structures & Preview)**: Collects all raw `Structure.Id` values and hit counts across matching plans, rendering a real-time preview of resolved aliases and status.
* **Rule Conflict Resolution (Specificity Priority + Top-First)**:
  1. **Exact Match (`Exact`)** evaluates first.
  2. **Contains Match (`Contains`)** evaluates second.
  3. **Regex Match (`Regex`)** evaluates third.
  4. Within the same match mode, the **topmost rule** in the list takes priority (reordered with Up/Down buttons).
  5. Duplicate rules with identical Pattern and MatchMode are automatically prevented.
* **Behavior with Multiple Raw Structures Mapped to the Same Target Alias**:
  * **Intentional Variation Consolidation**: Consolidates raw structures across patients into single CSV columns (e.g., `PTV-Volume[cc]`, `PTV-D95%[Gy]`).
  * **Collisions Within the Same Plan**: If multiple raw structures within a single plan match the same alias, the first matching structure is output to CSV, while JSON Lines preserves all structures hierarchically.
  * **Visual Annotation**: Discovered structures preview displays `[Merged: N]` when multiple raw structures map to the same alias.
* **Persistence**: Rules persist to JSON files.
* **One-Click Rule Creation**: Double-clicking a discovered structure row or clicking `+ Add to Rules` adds a new rule and refreshes the preview.
* **Bypass Option**: Supports bypassing pre-scan confirmation for routine workflows.

### 3.3. Extracted Parameters
* **Automatic Baseline DVH Statistics**: `Volume[cc]`, `Max dose[Gy]`, `Mean dose[Gy]`, and `Min dose[Gy]` are exported automatically for all mapped structures without manual DQP configuration.
* **Dynamic DQP Metrics**: 5-column format (`structureName, DQPtype, DQPvalue, InputUnit, OutputUnit`) supporting `Dose` (D), `Volume` (V), `DoseComplement` (DC), and `ComplementVolume` (CV).
* **Prescription Dose Normalization**: Relative dose parameters (e.g., V70%) normalize against the plan's Total Prescription Dose (`PlanSetup.TotalDose`).
* **Delivery Parameters**: Beam MU, Machine, Energy, Technique, PlanType (multi-beam records joined by semicolons).
* **Plan Metadata**: Calculation Model, Normalization Mode, Clinical Protocol, Optimization Objectives.
* **Calculation Logs**: Exported with newlines replaced by spaces.
* **Plan Complexity Analysis**: Computes literature-validated complexity indices (MCS / MCSv [Masi 2013, McNiven 2010], Edge Metric [Younge 2012], Leaf Travel Length, Arc Length).
* **De-Identification**: Replaces Patient ID with salted SHA-256 hash and masks birth dates and approver names as `REDACTED`.

## 4. Output Data Specifications
* **Unit Normalization (Gy)**: Converts `cGy` to `Gy` automatically. Column headers explicitly specify units (e.g., `[Gy]`).
* **CSV Format (1 plan = 1 row normalized flat format)**:
  * **1. Core Plan Information Columns (11 columns, always exported)**:
    * `Patient ID`, `Course ID`, `Date of birth`, `Plan ID`, `Target volume`, `DosePerFraction[Gy]`, `NumberOfFractions`, `TotalDose[Gy]`, `NumberOfBeams`, `ApprovalStatus`, `IsPlanSum`
  * **2. Optional Plan Metadata & Beam Parameter Columns (Up to 10 columns, enabled via Tab 4)**:
    * `PlanningApprover`, `PlanningApprovalDate`, `MU`, `Machine/Energy/Tech/PlanType`, `CalculationModel`, `CalculationLog`, `PlanNormalizationMethod`, `ClinicalProtocol`, `OptimizationObjectives`, `PlanComplexity` (MCS, EdgeMetric, LeafTravel, ArcLength, AAV, LSV)
  * **3. Automatic Baseline Structure Statistics (4 columns per mapped structure alias)**:
    * `<Alias>-Volume[cc]`, `<Alias>-Max dose[Gy]`, `<Alias>-Mean dose[Gy]`, `<Alias>-Min dose[Gy]`
  * **4. Dynamic Dose Quality Parameters (DQP) Columns (Configured in Tab 3)**:
    * `<Alias>-D<Val>%[Gy]`, `<Alias>-D<Val>cc[Gy]`, `<Alias>-V<Val>Gy[%]`, `<Alias>-DC<Val>%[Gy]`, `<Alias>-CV<Val>Gy[%]`, etc.
  * Multi-value fields (e.g., beam parameters, optimization objectives) are separated by semicolons (`;`).
  * Strings containing commas or newlines (e.g., calculation logs) have newlines converted to spaces, quotes escaped as `""`, and are wrapped in double quotes.
  * Missing or non-applicable values output as `N/A`.
  * Encoding: UTF-8 with BOM for native opening in Microsoft Excel.
* **JSONL Format**:
  * 1 JSON object per line preserving hierarchical structure (`Beams`, `OptimizationObjectives`) for ML/AI workflows.

## 5. ESAPI Implementation & Resource Safety
* **Deterministic Patient Disposal**: At most 1 patient in memory; calls `Application.ClosePatient()` in a `finally` block.
* **Periodic GC**: Invokes `System.GC.Collect()` every 200 patients.
* **Null Safety**: Comprehensive null checks for uncalculated doses, missing structure sets, and 3D-CRT plans without optimization.

## 6. UI & Design System Specifications
* **Design System**: Slate & Ocean Cyan Enterprise Theme.
* **Color Tokens**:
  * Primary: `#0284C7` (Sky-600)
  * Dark Header / Console: `#0F172A` (Slate-900)
  * Surface: `#FFFFFF`
  * Danger: `#EF4444` (Red-500)
  * Fonts: `Segoe UI` (UI), `Consolas` (Console & code)
* **Components**: Single-row action bar, 4 numbered tabs, dark terminal console, and overlaid progress bar.

## 7. Quality Assurance (QA) & Test Specifications
The automated testing pipeline (`test.bat`) executes MSBuild x64 Release builds and MSTest suites (116 tests, 100% pass) to ensure calculation accuracy and stability.
