# Changelog

**English** | [日本語](CHANGELOG.md)

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.1] - 2026-09-28

### Fixed
- **FIPS Policy Compatibility in Clinical Environments (`InvalidOperationException`)**:
  - Added `<enforceFIPSPolicy enabled="false"/>` to `App.config`, allowing Varian ESAPI internal communications (WCF / Gateway) to operate safely on clinical hospital workstations with FIPS policy enabled.
  - Migrated cryptographic hashing in `StringSanitizer.AnonymizePatientId` to Windows FIPS-certified providers (`SHA256CryptoServiceProvider` / `SHA256Cng`).
- **Release Artifacts Synchronization with Configuration File**:
  - Enhanced build pipeline (`test.bat` and `EclipseDataMiner.csproj` `PostBuild`) to automatically package `EclipseDataMiner_v3.0.1.exe.config` alongside `EclipseDataMiner_v3.0.1.exe` in `release/`.

### Added
- **Bilingual Documentation Across All Technical Documents**:
  - Full English translations for all technical documentation under `docs/` (`MANUAL.en.md`, `ARCHITECTURE.en.md`, `COMMISSIONING.en.md`, `CONTRIBUTING.en.md`, `DESIGN_SPECIFICATION.en.md`, `STANDARD_DEVELOPMENT_PLAN.en.md`, `TROUBLESHOOTING.en.md`, `CHANGELOG.en.md`).
  - Seamless language toggle links (`English | 日本語`) at the top of every document.
  - Comprehensive English code comments, XML docstrings, and test descriptions across all C# source files.
- **FIPS Troubleshooting Guide**:
  - Added Section 1.4 to `docs/TROUBLESHOOTING.md` and `docs/TROUBLESHOOTING.en.md` covering causes and solutions for FIPS algorithm exceptions.

### Changed
- **100% English Standardized UI**:
  - Unified all user interface components, buttons, tabs, filter boxes, tooltips, dialogs, and regex hints into standardized English.
- **License & Copyright Information Update**:
  - Updated `LICENSE` copyright period to `2019-2026 Takashi Kodama`.
  - Added `THIRD-PARTY-NOTICES.md` detailing open-source licenses and notices for bundled packages.

## [3.0.0] - 2026-09-26

### Added
- **"Dose" Column in Search Results Table**: New column displaying ✔ / — indicating whether 3D dose has been calculated.
- **Version Number in Release Artifacts**: Renamed standalone binary in `release/` to `EclipseDataMiner_v3.0.0.exe`.
- **Automatic PDF Manual Release Synchronization**: Auto-syncs PDF documentation to `release/` during `test.bat`.

### Changed
- **Major Version 3.0.0 Release**: Unified AssemblyVersion, InformationalVersion, UI title, and documentation to v3.0.0.

## [2.4.0] - 2026-09-25

### Added
- **Advanced Filters for Beam Parameters & Date Range**:
  - Machine ID, Energy, Technique filters supporting comma-separated OR and NOT (`!`, `-`) exclusions.
  - Date range filtering (`TreatmentApprovalDate`, `PlanningApprovalDate`, `CreationDate`).
- **Automated Startup Smoke Testing Pipeline & XAML Resource Integrity Guard**:
  - Automated smoke test launching the release binary post-build in `test.bat`.
  - Automated XAML `StaticResource` integrity validation unit test.
- **Folder-Based Search Preset System (`Presets/` Directory)**:
  - App-relative `Presets/` folder storage for seamless sharing and backup.
  - In-place description editing and clinical notes persistence.
- **Numerical Range & Inequality Filtering**:
  - Range (`70-80`, `70~80`) and inequality (`>=10`, `<=30`) syntax parser.
- **NOT / Exclusion Filters (`!QA`, `!Test`)**:
  - Exclusion syntax for all search text fields.
- **Modern Slate & Ocean Cyan Design System**:
  - Enterprise medical UI theme with optimized contrast and visual hierarchy.
