# EclipseDataMiner Clinical Commissioning Guide

**English** | [日本語](COMMISSIONING.md)

This document establishes the **acceptance criteria, verification checklists, and quality assurance procedures** required before deploying **EclipseDataMiner (v3.0 / compatible with Eclipse v15.6 & v16.1)** for clinical research and quality management.

> [!NOTE]
> All operations performed by EclipseDataMiner are strictly **Read-Only**. It never alters or saves patient or plan data (no `SaveModifications` calls). However, because it queries large volumes of treatment plans, clinical acceptance testing must be performed on a representative dataset in a test environment or TBox and approved by the Chief Medical Physicist before clinical adoption.

---

## Table of Contents

1. [Commissioning Principles & Standards](#1-commissioning-principles--standards)
2. [Test Environment & Prerequisites](#2-test-environment--prerequisites)
3. [Verification Checklists by Category](#3-verification-checklists-by-category)
   - 3.1 [Search & Filter Criteria Validation](#31-search--filter-criteria-validation)
   - 3.2 [Structure Pre-Scan & Nomenclature Mapping Validation](#32-structure-pre-scan--nomenclature-mapping-validation)
   - 3.3 [Baseline DVH Statistics & Dose Normalization (Gy)](#33-baseline-dvh-statistics--dose-normalization-gy)
   - 3.4 [Dynamic DQP Calculation Accuracy](#34-dynamic-dqp-calculation-accuracy)
   - 3.5 [Delivery Parameters & Modulation Complexity Analysis](#35-delivery-parameters--modulation-complexity-analysis)
   - 3.6 [Privacy Protection & Patient De-Identification](#36-privacy-protection--patient-de-identification)
   - 3.7 [Streaming Export Pipeline & JSONL Export](#37-streaming-export-pipeline--jsonl-export)
4. [Stress Testing, Failsafe, and Error Handling](#4-stress-testing-failsafe-and-error-handling)
5. [Clinical Acceptance Sign-Off Form](#5-clinical-acceptance-sign-off-form)

---

## 1. Commissioning Principles & Standards

Commissioning of EclipseDataMiner complies with established medical physics and radiation oncology safety standards:

- **AAPM TG-275**: Quality control and safety procedures for radiation oncology
- **AAPM TG-263**: Standardized nomenclature for structures and dose-volume parameters
- **ESTRO Booklet No. 9**: Guidelines for target volumes and organs at risk evaluation
- **AAPM TG-101 / ICRU Report 83**: Prescribing, recording, and reporting stereotactic and high-precision radiotherapy

### Core Guidelines
1. **Strict Concordance with Ground Truth**: Values extracted by EclipseDataMiner (Volume, Max dose, Mean dose, Min dose, D95%, V20Gy, etc.) must match the Eclipse DVH Statistics window within strict tolerance ($\le 0.01\text{ Gy}$ / $0.01\text{ cc}$ / $0.01\%$).
2. **Prescription-Based Relative Dose Normalization**: Relative dose parameters (e.g., V70%) must be normalized strictly against the plan's Total Prescription Dose (`TotalDose`).
3. **Non-Blocking Execution & Memory Safety**: Continuous scanning of 1,000+ plans must not freeze the UI or leak memory (Working Set must remain flat).
4. **Complete Privacy Protection**: When anonymization is enabled, raw Patient IDs, birth dates, and approver names must be completely excluded from CSV and JSONL exports.

---

## 2. Test Environment & Prerequisites

| Item | Requirement / Test Benchmark |
| :--- | :--- |
| **TPS Version** | Varian Eclipse v15.6 / v16.1 |
| **OS / Hardware** | Windows 10 / 11 64-bit, 16 GB+ RAM |
| **Runtime** | .NET Framework 4.6.1 or later |
| **User Rights** | ESAPI Scripting read permissions |
| **Benchmark Dataset** | ① Representative clinical cases (Prostate VMAT, Head & Neck IMRT, Lung SBRT)<br/>② Cases containing composite plan sums (`PlanSum`)<br/>③ Edge cases (uncalculated doses, unapproved plans, 3D-CRT plans without optimization) |

---

## 3. Verification Checklists by Category

### 3.1 Search & Filter Criteria Validation
- [ ] **Comma-Separated OR Search**: Querying multiple IDs (e.g., `ID001, ID002`) retrieves all matching cases without omission.
- [ ] **Global Logic Switch (AND / OR)**:
  - `AND` mode: Only plans meeting all criteria (dose, fractions, status, etc.) are matched.
  - `OR` mode: Plans meeting any individual criterion are matched.
- [ ] **Approval Status Filter**: Selection of `Unapproved`, `Plan approved`, and `TRT approved` filters plans correctly.
- [ ] **PlanSum Handling**:
  - `Include PlanSum` unchecked: Only `PlanSetup` instances are queried.
  - `Include PlanSum` checked: `PlanSum` instances are included; non-applicable parameters (e.g., beam logs) output `N/A` cleanly.

### 3.2 Structure Pre-Scan & Nomenclature Mapping Validation
- [ ] **High-Speed Pre-Scan**: Discovers all raw `Structure.Id` values within selected plans and counts occurrences accurately.
- [ ] **Exact Match**: Strict matching (e.g., `PTV-PROST`) correctly maps to the designated Target Alias.
- [ ] **Contains Match**: Substring matching (e.g., `prost`) consolidates variations into a unified alias.
- [ ] **Regex Match**: Regular expression patterns (e.g., `PTV.*|ptv.*`) match case variations and suffixes into a single `PTV` column.
- [ ] **Exclusion**: Unchecked structures (e.g., `BODY`, `CouchInterior`) are excluded from extraction.
- [ ] **Rule Persistence**: `Save Rules (JSON)` / `Load Rules (JSON)` reproduces rule dictionaries exactly (verified with `Templates/StructureMapping_Prostate.json`).

### 3.3 Baseline DVH Statistics & Dose Normalization (Gy)
- [ ] **Gy Unit Normalization**: When internal ESAPI values are in `cGy`, all CSV and JSONL outputs are converted to `Gy`.
- [ ] **Automatic 4 Baseline Metrics**: For every mapped structure, the following 4 columns are automatically output without explicit DQP registration:
  - `<Structure>-Volume[cc]`: Matches Eclipse structure volume ($\le 0.01\text{ cc}$).
  - `<Structure>-Max dose[Gy]`: Matches Eclipse DVH Max Dose ($\le 0.01\text{ Gy}$).
  - `<Structure>-Mean dose[Gy]`: Matches Eclipse DVH Mean Dose ($\le 0.01\text{ Gy}$).
  - `<Structure>-Min dose[Gy]`: Matches Eclipse DVH Min Dose ($\le 0.01\text{ Gy}$).

### 3.4 Dynamic DQP Calculation Accuracy
- [ ] **Dose at Volume (D)**:
  - `D95%[Gy]`: 95% volume dose matches Eclipse DVH.
  - `D0.1cc[Gy]`: Absolute volume 0.1 cc dose matches Eclipse DVH.
  - `D95%[%]`: Outputs relative percentage correctly.
- [ ] **Volume at Dose (V)**:
  - `V20Gy[%]`: 20 Gy irradiated volume percentage matches Eclipse DVH.
  - `V40Gy[cc]`: Volume in cc matches Eclipse DVH.
  - `V70%[%]`: Volume receiving 70% of prescription total dose is accurately computed.
- [ ] **Dose Complement (DC) & Complement Volume (CV)**:
  - Accurately computes dose complement and complement volume.
- [ ] **DQP Template**: `Templates/DQPlist_Prostate.csv` loads cleanly and generates expected columns.

### 3.5 Delivery Parameters & Modulation Complexity Analysis
- [ ] **Beam MU**: Total MU per beam is formatted as `B1:145.2;B2:138.6`.
- [ ] **Machine / Energy / Technique**: Machine name, energy mode (6X, 10X), delivery technique (ARC, STATIC), and plan type output correctly.
- [ ] **Complexity Metrics (MCS / Edge Metric)**: When enabled, Modulation Complexity Score and Edge Metric are computed accurately.
- [ ] **Calculation Log**: Algorithm logs are formatted with newlines replaced by spaces to maintain CSV integrity.

### 3.6 Privacy Protection & Patient De-Identification
- [ ] **Patient ID Hashing**: Patient IDs are converted to irreversible 64-character SHA-256 hashes. Identical IDs produce identical hashes.
- [ ] **PII Masking**: Birth dates and planning approver names are replaced with `REDACTED`.

### 3.7 Streaming Export Pipeline & JSONL Export
- [ ] **Instant Disk Flush**: Records are written and flushed immediately per plan.
- [ ] **JSON Lines Validity**: Output `.jsonl` file is parsable line-by-line using standard JSON libraries (Python `json.loads`).

---

## 4. Stress Testing, Failsafe, and Error Handling

- [ ] **High-Volume Continuous Scan**: Running 1,000+ plans maintains flat memory usage (<200 MB) without `OutOfMemoryException` (GC every 200 patients).
- [ ] **Cooperative Cancellation**: Clicking **"⏹ Cancel"** aborts execution within milliseconds without data corruption.
- [ ] **Missing Data Resilience**: Handles uncalculated plans, plans without structure sets, and 3D-CRT plans without optimization without crashes (outputs `N/A`).
- [ ] **File Lock Detection**: Displays clear error messages when the destination CSV file is locked in Microsoft Excel.

---

## 5. Clinical Acceptance Sign-Off Form

| Item | Evaluation | Date | Evaluator | Evidence / Notes |
| :--- | :---: | :---: | :---: | :--- |
| 1. Search Criteria & PlanSum | Pass / Fail | 2026/  /   | | |
| 2. Structure Pre-Scan & Aliases | Pass / Fail | 2026/  /   | | |
| 3. Baseline DVH Stats (Gy) | Pass / Fail | 2026/  /   | | |
| 4. Dynamic DQP Accuracy | Pass / Fail | 2026/  /   | | |
| 5. Delivery Parameters & MCS | Pass / Fail | 2026/  /   | | |
| 6. Privacy & De-Identification | Pass / Fail | 2026/  /   | | |
| 7. Streaming & JSONL Pipeline | Pass / Fail | 2026/  /   | | |
| 8. Resilience & Cancellation | Pass / Fail | 2026/  /   | | |

### Overall Assessment and Clinical Approval

EclipseDataMiner v3.0 has completed clinical acceptance testing and meets all requirements above. It is approved for clinical research and quality management operations.

- **Approval Date**: ______________________________
- **Chief Medical Physicist**: ______________________________ (Signature)
- **Department Director**: ______________________________ (Signature)
