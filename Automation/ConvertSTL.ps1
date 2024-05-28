# Main script to find STL files and convert them
param (
    [string]$FolderPath = "",
    [string]$PrusaSlicerPath = "path-to prusa-slicer.exe",
    [string]$MeshRepairPath = "path-to MeshRepair.exe"
)

function Convert-STLTo3MF {
    param (
        [string]$InputFile,
        [string]$PrusaSlicerPath
    )
    
    $outputFile = [System.IO.Path]::ChangeExtension($InputFile, ".3mf")

    if (-Not (Test-Path $PrusaSlicerPath)) {
        Write-Output "PrusaSlicer not found at $PrusaSlicerPath"
        exit
    }

    $arguments = @("--export-3mf", "--center", "0,0", "-o", "$outputFile", "$InputFile")
    & $PrusaSlicerPath $arguments 2>&1 | ForEach-Object { Write-Host $_ }
}

function Repair-3MF {
    param (
        [string]$InputFile,
        [string]$MeshRepairPath
    )
    
    if (-Not (Test-Path $MeshRepairPath)) {
        Write-Output "MeshRepair not found at $MeshRepairPath"
        exit
    }

    $arguments = @("--inputFilePath=`"$InputFile`"")
    & $MeshRepairPath $arguments 2>&1 | ForEach-Object { Write-Host $_ }

    # todo: implement exit codes
    # if ($LASTEXITCODE -eq 0) {
    #     Write-Host "Repaired: $InputFile" -ForegroundColor Green
    # } else {
    #     Write-Host "Failed to repair: $InputFile" -ForegroundColor Red
    # }
}


if (-Not (Test-Path $FolderPath)) {
    Write-Output "The folder path provided does not exist."
    exit
}

$startTime = Get-Date

# Get all .stl files in the folder and subfolders, don't see why this wouldn't work for other formats too as is
$stlFiles = Get-ChildItem -Path $FolderPath -Recurse -Filter *.stl

foreach ($file in $stlFiles) 
{
    $ThreeMF_file = [System.IO.Path]::ChangeExtension($file.FullName, ".3mf")

    if (Test-Path $ThreeMF_file) 
    {
        Write-Host "$ThreeMF_file already exists. Skipping conversion."
    }
    else
    {
        Convert-STLTo3MF -InputFile $file.FullName -PrusaSlicerPath $PrusaSlicerPath
    }

    if(Test-Path $ThreeMF_file)
    {
        Write-Host "Attempting repair on " -NoNewline
        Write-Host "$ThreeMF_file" -NoNewline -ForegroundColor Green
        Write-Host "..."
    
        Repair-3MF -InputFile $ThreeMF_file -MeshRepairPath $MeshRepairPath
    }
}

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "Conversion process completed in: $duration"
Read-Host "Press any key to close."
