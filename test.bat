@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo   EclipseDataMiner - Automated Verification Pipeline
echo ===================================================

set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSBUILD%" (
    set "MSBUILD=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)

set "VSTEST=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
if not exist "%VSTEST%" (
    set "VSTEST=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
)

if not exist "%MSBUILD%" (
    echo [ERROR] MSBuild.exe not found!
    exit /b 1
)

echo [1/4] Restoring NuGet Packages (/t:Restore)...
"%MSBUILD%" EclipseDataMiner.sln /t:Restore /verbosity:minimal
if %ERRORLEVEL% neq 0 (
    echo [ERROR] NuGet restore failed!
    exit /b 1
)

echo [2/4] Building Solution (x64 Release)...
"%MSBUILD%" EclipseDataMiner.sln /p:Configuration=Release /p:Platform=x64 /t:Build /verbosity:minimal
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    exit /b 1
)

if not exist "%VSTEST%" (
    echo [WARNING] vstest.console.exe not found at %VSTEST%. Build succeeded.
    exit /b 0
)

echo [3/4] Running MSTest Suite...
"%VSTEST%" EclipseDataMiner.Tests\bin\x64\Release\EclipseDataMiner.Tests.dll /Platform:x64
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Tests failed!
    exit /b 1
)

echo [4/5] Synchronizing Release Artifacts (release/)...
if not exist "release" mkdir release
copy /y "EclipseDataMiner\bin\x64\Release\EclipseDataMiner.exe" "release\EclipseDataMiner_v3.0.exe" >nul
if exist "EclipseDataMiner\Templates" (
    if not exist "release\Templates" mkdir "release\Templates"
    xcopy /y /e /i "EclipseDataMiner\Templates" "release\Templates" >nul
)
if not exist "release\Presets" mkdir "release\Presets"
if exist "EclipseDataMiner\Presets" (
    copy /y "EclipseDataMiner\Presets\*.json" "release\Presets\" >nul
)
if exist "EclipseDataMiner_Manual.pdf" (
    copy /y "EclipseDataMiner_Manual.pdf" "release\" >nul
)

echo [5/5] Smoke Testing Release Binary Startup and UI Load...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase; try { [System.Reflection.Assembly]::LoadFrom((Resolve-Path 'release\EclipseDataMiner_v3.0.exe')) | Out-Null; $app = New-Object EclipseDataMiner.App; $app.InitializeComponent(); $win = New-Object EclipseDataMiner.MainWindow; Write-Host '  [OK] MainWindow and all XAML styles loaded without exception.' } catch { Write-Error ('XAML/Style resolution failed: ' + $_.Exception.ToString()); exit 1 }"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Release binary UI failed to load!
    exit /b 1
)

echo ===================================================
echo   [SUCCESS] All 100%% Tests Passed and Binary Verified! Release Ready.
echo ===================================================
endlocal
