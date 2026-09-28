# EclipseDataMiner (v3.0.1)

**English** | [日本語](README.md)

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Platform](https://img.shields.io/badge/.NET%20Framework-4.6.1-blue.svg)]()
[![Target](https://img.shields.io/badge/Architecture-x64-orange.svg)]()
[![Eclipse](https://img.shields.io/badge/Eclipse-v15.6%20%7C%20v16.1-purple.svg)]()
[![Tests](https://img.shields.io/badge/MSTest-117%2F117%20PASS-success.svg)]()
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A standalone **10,000-plan-scale clinical data mining application** running on Varian Eclipse (ESAPI).  
It cross-searches patient databases for treatment plans (PlanSetup / PlanSum) matching complex clinical criteria, extracts dose-quality parameters (DQP), delivery parameters, clinical protocols, optimization objectives, and modulation complexity metrics (Modulation Complexity Score, Edge Metric, etc.) with high throughput and memory safety, and streams results into normalized CSV and machine-learning-ready JSON Lines (`.jsonl`) files.

---

## 🌟 Key Features (v3.0.1)

- 🌐 **Standard English UI & Bilingual Documentation**:
  - Clean, international-standard English UI designed for radiation oncology clinical workflows.
  - Comprehensive technical documentation and user manuals provided in both English and Japanese.
- 🚀 **10,000-Scale High-Throughput Streaming Pipeline**:
  - Immediately flushes extracted data to disk per plan/patient without accumulating in memory, maintaining a flat memory footprint (~few tens of MBs) even during long runs.
- 🔍 **Structure Pre-Scan & Multi-Rule Alias Mapping**:
  - High-speed pre-scan of structure IDs across matching plans, aggregating nomenclature variations (`PTV_60`, `ptv60`, `PTV-60Gy`, etc.) into unified columns via Exact, Substring, or Regex matching.
  - Reusable JSON rule persistence.
- 🔀 **Hierarchical AND / OR Filtering & Dose Presence Logic**:
  - Comma-delimited intra-field OR search combined with top-level AND / OR toggle switch.
  - Dose status filter (`All`, `HasDose` [calculated], `NoDose` [uncalculated]).
  - Numeric range expressions (`70-80`) and inequality filters (`>=10`) for doses and fraction counts.
  - Delivery parameter filters (Machine, Energy, Technique) and date range filtering.
  - Support for composite plans (`PlanSum`).
- 🤖 **Simultaneous JSON Lines (`.jsonl`) Streaming for AI / ML**:
  - Preserves hierarchical plan data (beams, optimization constraints, DVH metrics) ready for direct ingestion in Python (Pandas, PyTorch, TensorFlow).
- 🔒 **Privacy Protection & Patient De-Identification**:
  - SHA-256 patient ID hashing and automatic `REDACTED` masking of personal data (birth dates, approver names) for collaborative and external clinical research.
- 📏 **Automatic Dose Unit Standardization (Unified Gy)**:
  - Automatically identifies ESAPI `cGy` / `Gy` inconsistencies and standardizes all values to `Gy`.
- 🛡️ **Robust Dedicated STA Worker Thread & Memory Safety**:
  - Background STA thread prevents UI freezing during intensive ESAPI queries.
  - Periodic `GC.Collect()` every 200 patients and strict `try-finally` object disposal to eliminate memory leaks.
- 📦 **Single Standalone Executable (Costura.Fody)**:
  - All external NuGet dependencies are bundled inside `EclipseDataMiner.exe`. Ready to run on any clinical Eclipse workstation without installer or DLL deployment.

---

## 🖥️ System Requirements

- **Operating System**: Windows 10 / 11 / Windows Server (64-bit)
- **Treatment Planning System**: Varian Eclipse v15.6 / v16.1
- **Runtime**: .NET Framework 4.6.1 or higher
- **Permissions**: ESAPI script execution permissions (Run on clinical workstation or TBox)

---

## 🚀 Quick Start & Installation

This tool is distributed as a single standalone executable.

1. **Obtain the Release Package**:
   - Download the contents of the `release\` folder (or `release\EclipseDataMiner_v3.0.1.exe` and `EclipseDataMiner_v3.0.1.exe.config`).
2. **Deploy to Eclipse Workstation**:
   - Copy the executable and configuration file (`EclipseDataMiner_v3.0.1.exe` and `EclipseDataMiner_v3.0.1.exe.config`), along with `Presets\` and `Templates\` folders, to any directory on your Eclipse workstation.
   - *Note: In clinical hospital networks with FIPS enforcement enabled, the `.config` file is strictly required alongside the `.exe` to bypass FIPS restrictions.*
3. **Launch**:
   - Double-click `EclipseDataMiner_v3.0.1.exe` to launch.

---

## 🖥️ User Interface Overview

![EclipseDataMiner UI](img/UI.png)

---

## 📖 Operational Workflow

The application UI consists of 4 sequential tabs, guiding users intuitively from left to right:

```
[1- Plan Search]       ->  [2- Structure Mapping]  ->  [3- DQP Configuration]
 Define filter criteria     Scan patient structures     Define DVH parameters
 Search & select plans      Map aliases & variations    (e.g., D95%, V20Gy)
        ↓
[4- Options & Export]  ->  [Bottom Control Bar]
 Specify CSV / JSONL path   [▶ Run Extraction] One-click execution
 Complexity & anonymize     Real-time progress bar & console logs
```

1. **Tab 1: 1- 📋 Plan Search**:
   - Configure search criteria (Patient ID, Plan ID, Dose status, Approval status, Machine, Dates) or load a saved Preset.
   - Click **"🔍 Search Plans"** to scan the database, then select target plans in the results data grid.
2. **Tab 2: 2- 📐 Structure Mapping**:
   - Click **"🔍 Pre-Scan Structures"** to discover structure names across target plans and consolidate variations into unified aliases.
3. **Tab 3: 3- 📊 Dose Quality Parameters (DQP)**:
   - Configure dose/volume evaluation metrics (e.g., D95%, V20Gy, Mean, Max). Supports CSV import/export.
4. **Tab 4: 4- ⚙️ Extraction & Analysis Options**:
   - Choose output CSV destination path (`Browse...` / `Open Folder`).
   - Enable Beam Parameters, Plan Complexity (MCS, Edge Metric), Anonymization, and JSONL output options.
5. **Execute Extraction (Bottom Bar)**:
   - Click **"▶ Run Extraction"** to start the extraction pipeline. Monitor live progress and execution logs in the bottom status panel.

---

## 📊 Export Data Specifications

### 1. CSV Output (Flat Format)
- One row per plan (comma-separated, UTF-8 with BOM).
- Structured in 4 sections: Core columns (11), Optional metadata/parameters (up to 10), Baseline structure statistics (4 per structure), and Dynamic DQP metrics. For a complete column-by-column reference, see [User Manual Section 4 (docs/MANUAL.en.md#4)](docs/MANUAL.en.md#4).
- Newline characters are replaced with spaces; commas and quotes are escaped properly.
- Missing or non-applicable values are exported as `N/A`.

| Column Name (Examples) | Description | Example |
| :--- | :--- | :--- |
| `Patient ID` | Patient identifier (SHA-256 hash if anonymized) | `PT_1001` or `e3b0c442...` |
| `Course ID` / `Plan ID` | Course and Plan IDs | `C1`, `VMAT_Prostate` |
| `DosePerFraction[Gy]` | Prescription dose per fraction (Gy) | `2.00` |
| `TotalDose[Gy]` | Total prescription dose (Gy) | `60.00` |
| `ApprovalStatus` | Clinical approval status | `TreatmentApproved` |
| `MU` | Beam MU values (semicolon-separated) | `B1:145.2;B2:138.6` |
| `Machine/Energy/Tech/PlanType` | Delivery parameters summary | `TrueBeam:6X:ARC:VMAT` |
| `PlanComplexity` | Modulation metrics (Beam:MCS,EM,LeafTravel,ArcLength) | `(B1:0.28,0.12,1850.4,360.0)` |
| `<Structure>-Volume[cc]` | Structure volume (auto-exported) | `42.50` |
| `<Structure>-Max dose[Gy]` | Structure maximum dose (auto-exported) | `64.20` |
| `<Structure>-Mean dose[Gy]` | Structure mean dose (auto-exported) | `61.80` |
| `<Structure>-Min dose[Gy]` | Structure minimum dose (auto-exported) | `58.10` |
| `<Structure>-D95%[Gy]` | Calculated DQP dose value | `59.80` |
| `<Structure>-V70%[%]` | Calculated DQP relative volume value | `18.50` |

### 2. JSON Lines (`.jsonl`) Output
- One complete JSON object per line.
- Easily loadable into Python:
  ```python
  import json
  with open("DataMiningOutput.csv.jsonl", "r", encoding="utf-8") as f:
      plans = [json.loads(line) for line in f]
  print(f"Loaded {len(plans)} plans.")
  ```

---

## 📚 Technical Documentation Suite

| Document | Description |
| :--- | :--- |
| 📖 **User Manual** | [English (docs/MANUAL.en.md)](docs/MANUAL.en.md) \| [日本語 (docs/MANUAL.md)](docs/MANUAL.md) — Complete step-by-step UI guide, DQP templates, Python integration examples |
| 📐 **Architecture Specification** | [English (docs/ARCHITECTURE.en.md)](docs/ARCHITECTURE.en.md) \| [日本語 (docs/ARCHITECTURE.md)](docs/ARCHITECTURE.md) — Layered architecture, STA worker model, memory management, and sequence diagrams |
| 🏥 **Clinical Commissioning Protocol** | [English (docs/COMMISSIONING.en.md)](docs/COMMISSIONING.en.md) \| [日本語 (docs/COMMISSIONING.md)](docs/COMMISSIONING.md) — TG-275-compliant clinical commissioning guide, validation checklists, approval forms |
| 🔧 **Troubleshooting & FAQ** | [English (docs/TROUBLESHOOTING.en.md)](docs/TROUBLESHOOTING.en.md) \| [日本語 (docs/TROUBLESHOOTING.md)](docs/TROUBLESHOOTING.md) — Common issues, error resolutions (file locks, memory, ESAPI connection), FAQ |
| 🤝 **Contributing Guidelines** | [English (docs/CONTRIBUTING.en.md)](docs/CONTRIBUTING.en.md) \| [日本語 (docs/CONTRIBUTING.md)](docs/CONTRIBUTING.md) — Environment setup, build/test pipelines, coding conventions, git workflow |
| 📋 **Design Specification** | [English (docs/DESIGN_SPECIFICATION.en.md)](docs/DESIGN_SPECIFICATION.en.md) \| [日本語 (docs/DESIGN_SPECIFICATION.md)](docs/DESIGN_SPECIFICATION.md) — Comprehensive software specifications and ESAPI control specifications |
| 🛡️ **Standard Development Plan** | [English (docs/STANDARD_DEVELOPMENT_PLAN.en.md)](docs/STANDARD_DEVELOPMENT_PLAN.en.md) \| [日本語 (docs/STANDARD_DEVELOPMENT_PLAN.md)](docs/STANDARD_DEVELOPMENT_PLAN.md) — 7 Core Quality Principles, 4-tier DoD gates, and reproducibility protocols |
| 📝 **Changelog** | [English (docs/CHANGELOG.en.md)](docs/CHANGELOG.en.md) \| [日本語 (docs/CHANGELOG.md)](docs/CHANGELOG.md) — Version-by-version release history (Keep a Changelog standard) |
| 📄 **Third-Party Notices** | [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) — Open-source software licenses and copyright acknowledgments |
| 📖 **Consolidated Technical Manual PDF** | [English (release/EclipseDataMiner_v3.0.1_Manual.en.pdf)](release/EclipseDataMiner_v3.0.1_Manual.en.pdf) \| [日本語 (release/EclipseDataMiner_v3.0.1_Manual.pdf)](release/EclipseDataMiner_v3.0.1_Manual.pdf) — Unified publication-quality manual formatted for A4 printing |

---

## 🧪 Testing & Quality Assurance

This repository includes a multi-layered automated test suite using `EclipseDataMiner.Tests` (MSTest), verifying all business and numerical logic without requiring an active ESAPI database connection.

- **Total Unit Tests**: 117 tests (100% PASS)
- **Coverage**: Dose normalization (`ToGy()`), string sanitization, patient de-identification, streaming CSV/JSONL pipelines, hierarchical AND/OR search logic with UI synchronization, dose presence filter (`HasDose`/`NoDose`), inequality/range parsing, beam/date filters, preset persistence, 2-pane structure mapping with real-time preview, rule priority resolution (Exact > Contains > Regex), complexity algorithms benchmarked against published literature (MCS, Edge Metric, Leaf Travel Length, Arc Length), XAML resource integrity, and UI screenshot generation.

---

## 📄 License

This software is released under the [MIT License](LICENSE).
For third-party open-source components and licenses included in this project, please refer to [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
