# EclipseDataMiner Troubleshooting & FAQ Guide

**English** | [日本語](TROUBLESHOOTING.md)

This document provides solutions and diagnostics for common startup, configuration, UI, filtering, structure mapping, DQP metric calculation, and streaming export issues encountered when using **EclipseDataMiner (v3.0 / Eclipse v15.6 & v16.1)**.

---

## Table of Contents

1. [Startup & Environment Issues](#1-startup--environment-issues)
   - 1.1 [ESAPI DLL Missing (`FileNotFoundException`)](#11-esapi-dll-missing-filenotfoundexception)
   - 1.2 [.NET Framework Version Mismatch](#12-net-framework-version-mismatch)
   - 1.3 [Scripting Permissions / Standalone Access Denied](#13-scripting-permissions--standalone-access-denied)
   - 1.4 [FIPS Cryptographic Algorithm Policy Exception (`InvalidOperationException`)](#14-fips-cryptographic-algorithm-policy-exception-invalidoperationexception)
2. [Search & Extraction Issues](#2-search--extraction-issues)
   - 2.1 [UI Freezing or Becoming Unresponsive](#21-ui-freezing-or-becoming-unresponsive)
   - 2.2 [Out of Memory (`OutOfMemoryException`) on Large Datasets](#22-out-of-memory-outofmemoryexception-on-large-datasets)
   - 2.3 [Safely Aborting Extraction / Data Preservation on Cancellation](#23-safely-aborting-extraction--data-preservation-on-cancellation)
   - 2.4 [Extraction Aborting on Specific Patients or Corrupted Plans](#24-extraction-aborting-on-specific-patients-or-corrupted-plans)
3. [DQP, Structure Mapping, and Export Issues](#3-dqp-structure-mapping-and-export-issues)
   - 3.1 [All DQP Columns Output `N/A` in CSV](#31-all-dqp-columns-output-na-in-csv)
   - 3.2 [Structure Variations Not Consolidating](#32-structure-variations-not-consolidating)
   - 3.3 ["The process cannot access the file because it is being used by another process"](#33-the-process-cannot-access-the-file-because-it-is-being-used-by-another-process)
   - 3.4 [CSV Text Encoding / Japanese Garbled in Excel](#34-csv-text-encoding--japanese-garbled-in-excel)
4. [Frequently Asked Questions (FAQ)](#4-frequently-asked-questions-faq)

---

## 1. Startup & Environment Issues

### 1.1 ESAPI DLL Missing (`FileNotFoundException`)
- **Symptom**: Launching `EclipseDataMiner.exe` triggers `Could not load file or assembly 'VMS.TPS.Common.Model.API.dll'` and terminates immediately.
- **Cause**:
  - The application is running on a non-Eclipse workstation or the ESAPI DLLs are not in the standard GAC or application directory.
  - ESAPI DLLs are excluded from `Costura.Fody` embedding due to Varian licensing policies.
- **Resolution**:
  - **Run on Authorized Workstations**: Run the executable on an official Eclipse clinical workstation or TBox.
  - **Manual DLL Placement**: For development/testing environments, copy `VMS.TPS.Common.Model.API.dll` and `VMS.TPS.Common.Model.Types.dll` into the same folder as `EclipseDataMiner.exe`.

### 1.2 .NET Framework Version Mismatch
- **Symptom**: `.NET Framework Initialization Error` appears on launch.
- **Cause**: The workstation does not have .NET Framework 4.6.1 or later installed.
- **Resolution**: Install .NET Framework 4.6.1 or later (4.8 recommended) via Windows Features or the Microsoft website.

### 1.3 Scripting Permissions / Standalone Access Denied
- **Symptom**: Application fails with `Access Denied` or `User has insufficient rights`.
- **Cause**: The current Windows user account does not have ESAPI standalone read access.
- **Resolution**: Request your Eclipse System Administrator to grant ESAPI read rights to the account.

### 1.4 FIPS Cryptographic Algorithm Policy Exception (`InvalidOperationException`)
- **Symptom**: 
  - Works normally on research/testing workstations, but fails in clinical production environments with the following error:
  ```text
  Error occured while processing request
  System.InvalidOperationException: This implementation is not part of the Windows Platform FIPS validated cryptographic algorithms.
  ```
- **Cause**: 
  - Hospital clinical workstations enforce the Group Policy or Local Security Policy: **"System cryptography: Use FIPS compliant algorithms for encryption, hashing, and signing"** (`FipsAlgorithmPolicy = 1`).
  - When this policy is enabled, .NET Framework blocks instantiation of standard managed cryptographic classes (such as `SHA256Managed`), impacting Varian ESAPI internal communications (WCF / Gateway token authentication) and patient de-identification hashing.
- **Resolution**:
  1. **Deploy Configuration File (`EclipseDataMiner.exe.config`) Together (Recommended)**:
     - Always deploy `EclipseDataMiner.exe.config` in the **exact same folder** as `EclipseDataMiner.exe`.
     - The `<enforceFIPSPolicy enabled="false"/>` directive in the config file allows .NET runtime to safely bypass the OS-level FIPS enforcement for ESAPI internal operations.
     - *(Note: Starting in v3.0, the application code also uses FIPS-certified providers `SHA256CryptoServiceProvider` / `SHA256Cng` for de-identification hashing).*
  2. **Disable FIPS Policy on the Workstation (If Administrator Rights are Available)**:
     - Open `secpol.msc` (Local Security Policy).
     - Navigate to [Security Settings] → [Local Policies] → [Security Options].
     - Double-click **"System cryptography: Use FIPS compliant algorithms for encryption, hashing, and signing"** and set it to **Disabled**. Reboot or run `gpupdate /force`.
     - *(Consult your hospital IT department before modifying domain policies).*

---

## 2. Search & Extraction Issues

### 2.1 UI Freezing or Becoming Unresponsive
- **Symptom**: Window title shows "Not Responding" during long queries.
- **Cause**: In legacy versions (v1.0), queries ran directly on the UI thread.
- **Resolution**: Update to **v3.0**. All queries run on an isolated background STA worker thread (`StaEsapiWorkerService`). The UI remains smooth and responsive throughout.

### 2.2 Out of Memory (`OutOfMemoryException`) on Large Datasets
- **Symptom**: Memory usage continuously escalates until the application crashes.
- **Cause**: Accumulating thousands of plan records in RAM.
- **Resolution**: v3.0 employs a streaming pipeline. Only 1 patient is held in memory at a time, records are immediately flushed to disk, and `GC.Collect()` is explicitly called every 200 patients. Working set memory stays flat (<200 MB).

### 2.3 Safely Aborting Extraction / Data Preservation on Cancellation
- **Symptom**: Need to stop a running query without losing progress.
- **Resolution**: Click the red **"⏹ Cancel"** button. The background loop cooperatively terminates within milliseconds. All records written prior to cancellation remain safely stored in the output CSV and JSONL files.

### 2.4 Extraction Aborting on Specific Patients or Corrupted Plans
- **Symptom**: A single corrupted plan crashes the entire run.
- **Resolution**: v3.0 wraps each patient in comprehensive `try-catch` blocks. If an uncalculated or corrupted plan is encountered, an error message is logged to the console, non-applicable fields are filled with `N/A`, and execution safely proceeds to the next patient.

---

## 3. DQP, Structure Mapping, and Export Issues

### 3.1 All DQP Columns Output `N/A` in CSV
- **Symptom**: Baseline metadata is exported, but DQP columns contain only `N/A`.
- **Cause**: The structure name specified in the DQP list does not match the resolved Target Alias from Step 2.
- **Resolution**: Verify that the Structure Name in Tab 3 exactly matches the `Target Alias` defined in Tab 2.

### 3.2 Structure Variations Not Consolidating
- **Symptom**: `PTV_60` and `ptv-60` output as separate columns.
- **Cause**: Rules in Tab 2 are configured with `Exact` mode instead of `Contains` or `Regex`.
- **Resolution**: Change the Match Mode to `Contains` (e.g., `PTV` with alias `PTV`) or use a regex pattern (e.g., `PTV.*`).

### 3.3 "The process cannot access the file because it is being used by another process"
- **Symptom**: Export fails immediately upon start.
- **Cause**: The destination CSV file is open in Microsoft Excel or another program.
- **Resolution**: Close Excel or specify a new file name using **"Browse..."** in Tab 4.

### 3.4 CSV Text Encoding / Japanese Garbled in Excel
- **Symptom**: Non-ASCII characters display incorrectly in Microsoft Excel.
- **Cause**: Excel expects a UTF-8 BOM when opening CSV files by double-clicking.
- **Resolution**: EclipseDataMiner exports UTF-8 with BOM by default. In Excel, you can also use `Data > From Text/CSV` and select UTF-8 encoding.

---

## 4. Frequently Asked Questions (FAQ)

- **Q: Does EclipseDataMiner modify clinical data in Eclipse?**  
  **A**: Absolutely not. EclipseDataMiner is strictly **Read-Only** and never invokes `SaveModifications()`.

- **Q: Can I run queries across multiple LINACs?**  
  **A**: Yes. Use the `Machine ID` filter under `▾ Advanced Filters` (e.g., `TrueBeam, Clinac_iX`).

- **Q: Where are my search presets stored?**  
  **A**: Presets are saved in the `Presets/` folder located in the same directory as `EclipseDataMiner.exe`. You can easily copy and share them between workstations.
