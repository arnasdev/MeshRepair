$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore

Write-Host "Building MeshRepair (Release)..." -ForegroundColor Cyan
dotnet build -c Release

$exePath = ".\MeshRepair\bin\Release\net8.0-windows10.0.19041.0\win-x64\MeshRepair.exe"

if (Test-Path $exePath) {
    Write-Host "Build successful! Executable located at: $exePath" -ForegroundColor Green
    
    # Check for test input
    $testInput = ".\MeshRepair\bin\Release\net8.0-windows10.0.19041.0\win-x64\input\Corner_south_east_Grid3_C0.stl"
    
    if (Test-Path $testInput) {
        Write-Host "Found test input: $testInput" -ForegroundColor Cyan
        Write-Host "Running test (Repairing single file)..." -ForegroundColor Cyan
        
        # Create a temp output folder
        $testOutput = ".\test_output"
        if (!(Test-Path $testOutput)) { New-Item -ItemType Directory -Path $testOutput | Out-Null }
        
        # Run tool
        & $exePath --inputFilePath "$testInput" --outputFilePath "$testOutput" --outputFormat "stl"
        
        Write-Host "`nTest run completed." -ForegroundColor Green
    } else {
        Write-Host "No test input found at standard location, running help..." -ForegroundColor Yellow
        & $exePath --help
    }
} else {
    Write-Host "Build failed or executable not found at expected path." -ForegroundColor Red
    exit 1
}
