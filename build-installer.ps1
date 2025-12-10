$ErrorActionPreference = "Stop"

# Configuration
$SolutionPath = ".\MeshRepair\MeshRepair.csproj"
$InstallerScript = ".\installer\setup.iss"
$InnoSetupCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" # Standard path

# 1. Check for Inno Setup
if (!(Test-Path $InnoSetupCompiler)) {
    Write-Host "Inno Setup Compiler (ISCC.exe) not found at default location: $InnoSetupCompiler" -ForegroundColor Red
    Write-Host "Please ensure Inno Setup 6 is installed." -ForegroundColor Yellow
    exit 1
}

# 2. Restore and Build Solution
Write-Host "`n=== Step 1: Building MeshRepair v3.1.0 ===" -ForegroundColor Cyan
Write-Host "Restoring packages..."
dotnet restore $SolutionPath

Write-Host "Cleaning previous builds..."
dotnet clean $SolutionPath -c Release

Write-Host "Building Release (win-x64)..."
dotnet build $SolutionPath -c Release -r win-x64

# Verify Build
$exePath = ".\MeshRepair\bin\Release\net8.0-windows10.0.19041.0\win-x64\MeshRepair.exe"
if (!(Test-Path $exePath)) {
    Write-Host "Build failed! Executable not found at $exePath" -ForegroundColor Red
    exit 1
}
Write-Host "Build Successful!" -ForegroundColor Green

# 3. Build Installer
Write-Host "`n=== Step 2: Compiling Installer ===" -ForegroundColor Cyan
$setupCmd = "& '$InnoSetupCompiler' '$InstallerScript'"
Invoke-Expression $setupCmd

# Verify Installer
$installerDir = ".\releases"
$installerPattern = "MeshRepair-Setup-v3.1.0.exe"
$installerPath = Join-Path $installerDir $installerPattern

if (Test-Path $installerPath) {
    Write-Host "`n[SUCCESS] Installer created successfully!" -ForegroundColor Green
    Write-Host "Location: $installerPath" -ForegroundColor Green
} else {
    # Check if a file was created in releases even if name differs
    $files = Get-ChildItem -Path $installerDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending
    if ($files) {
        Write-Host "`n[SUCCESS] Installer created (filename might differ):" -ForegroundColor Green
        Write-Host "Location: $($files[0].FullName)" -ForegroundColor Green
    } else {
        Write-Host "`n[ERROR] Installer compilation seemed to complete but output file not found." -ForegroundColor Red
        exit 1
    }
}
