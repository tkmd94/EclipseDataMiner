# EclipseDataMiner Contributing Guide

**English** | [日本語](CONTRIBUTING.md)

This document provides developer guidelines, build instructions, coding standards, and testing procedures for contributors and medical physicists developing **EclipseDataMiner**.

---

## Table of Contents

1. [Development Environment Requirements](#1-development-environment-requirements)
2. [Repository Setup and Build Instructions](#2-repository-setup-and-build-instructions)
3. [Automated Verification Pipeline (`test.bat`)](#3-automated-verification-pipeline-testbat)
4. [Architectural Design Guidelines](#4-architectural-design-guidelines)
5. [Coding Standards](#5-coding-standards)
6. [Commit Message Conventions (Conventional Commits)](#6-commit-message-conventions-conventional-commits)
7. [Pull Request (PR) Checklist](#7-pull-request-pr-checklist)

---

## 1. Development Environment Requirements

- **Operating System**: Windows 10 / 11 (64-bit)
- **IDE**: Visual Studio 2022 Community / Professional or later
  - Required Workload: **.NET desktop development**
  - .NET Framework 4.6.1 Developer Pack / Targeting Pack
- **Target Platform**: `.NET Framework 4.6.1` / `x64`
- **Language Version**: C# 10.0 (`<LangVersion>10.0</LangVersion>`)
- **External ESAPI Libraries**:
  - `VMS.TPS.Common.Model.API.dll` (v15.6 or v16.1)
  - `VMS.TPS.Common.Model.Types.dll` (v15.6 or v16.1)
  - *Project files automatically probe the following fallback paths:*
    1. `C:\Program Files\Varian\RTM\16.1\esapi\API`
    2. Parent repository directory (`..\..\`)
    3. Solution parent directory (`$(SolutionDir)..`)

---

## 2. Repository Setup and Build Instructions

### NuGet Package Restoration
The solution uses standard `PackageReference` format. Run MSBuild to restore packages:

```powershell
# Restore NuGet packages
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    EclipseDataMiner.sln /t:Restore /verbosity:minimal
```

### Building the Solution
ESAPI is a native 64-bit library; always build for the **`x64`** platform:

```powershell
# x64 Release Build
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    EclipseDataMiner.sln /p:Configuration=Release /p:Platform=x64 /t:Build /verbosity:minimal
```

The resulting standalone executable is generated at `EclipseDataMiner\bin\x64\Release\EclipseDataMiner.exe` (~590 KB).

---

## 3. Automated Verification Pipeline (`test.bat`)

Always run `test.bat` before submitting changes:

```cmd
test.bat
```

The pipeline executes 5 automated stages:
1. **NuGet Package Restoration**
2. **x64 Release MSBuild Compilation**
3. **MSTest Unit Test Execution (116/116 tests 100% pass)**
4. **Release Artifact Synchronization**
5. **Release Binary Startup Smoke Testing**

---

## 4. Architectural Design Guidelines

1. **Decoupled Intermediate DTOs**: Map ESAPI objects into independent DTOs (`ExtractionPlanRecord`). Do not pass ESAPI objects to serialization or UI layers.
2. **Dedicated STA Thread**: Keep all ESAPI invocations isolated on a Single-Threaded Apartment worker thread (`StaEsapiWorkerService`).
3. **Deterministic Memory Cleanup**: Deterministically close patients inside `try-finally` blocks and call `GC.Collect()` periodically.

---

## 5. Coding Standards

- **Language**: All code comments, XML docstrings, and test descriptions must be written in **English**.
- **Naming**: Follow standard Microsoft C# naming conventions (PascalCase for classes/methods, camelCase for local variables).
- **Null Safety**: Perform strict null checks on all nullable ESAPI properties.
- **Unit Testing**: Adhere to the Arrange-Act-Assert (AAA) pattern.

---

## 6. Commit Message Conventions (Conventional Commits)

Format commit messages according to the Conventional Commits specification:

```
<type>(<scope>): <short description>
```

- `feat`: New clinical feature or extraction metric
- `fix`: Bug fix or logic correction
- `docs`: Documentation updates
- `test`: Adding or modifying unit tests
- `refactor`: Code improvements without behavior changes

---

## 7. Pull Request (PR) Checklist

- [ ] `test.bat` passes 100% (all 116 unit tests pass).
- [ ] No compilation warnings on x64 Release.
- [ ] Code comments are written in English.
- [ ] Documentation (Markdown) is updated in both English and Japanese.
- [ ] No institutional passwords, local paths, or patient identifiers are committed.
