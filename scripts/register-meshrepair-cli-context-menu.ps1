# register-meshrepair-cli-context-menu.ps1
# Script para agregar "Abrir MeshRepair CLI aqui" al menu contextual de carpetas

$meshRepairPath = "C:\Program Files\MeshRepair\MeshRepair.exe"

# Verificar si MeshRepair.exe existe
if (-not (Test-Path $meshRepairPath)) {
    Write-Host "Error: MeshRepair.exe no encontrado en $meshRepairPath" -ForegroundColor Red
    Write-Host "Verifica la ruta de instalacion" -ForegroundColor Yellow
    exit 1
}

# Crear entrada en Registry para el menu contextual de carpetas (click derecho en espacio vacio)
try {
    # Usar la ruta completa del Registry en lugar de HKCR: drive
    $registryPath = "Registry::HKEY_CLASSES_ROOT\Directory\Background\shell\MeshRepairCLI"
    $commandPath = "Registry::HKEY_CLASSES_ROOT\Directory\Background\shell\MeshRepairCLI\command"
    
    # Crear la entrada principal
    New-Item -Path $registryPath -Force | Out-Null
    Set-ItemProperty -Path $registryPath -Name "(Default)" -Value "Abrir MeshRepair CLI aqui"
    Set-ItemProperty -Path $registryPath -Name "Icon" -Value "`"$meshRepairPath`",0"

    # Crear el comando que se ejecuta
    New-Item -Path $commandPath -Force | Out-Null
    Set-ItemProperty -Path $commandPath -Name "(Default)" -Value "cmd.exe /k cd /d `"%V`" && meshrepair -d"

    Write-Host "MeshRepair CLI agregado al menu contextual exitosamente!" -ForegroundColor Green
    Write-Host "  Ruta: $meshRepairPath" -ForegroundColor Cyan
    Write-Host "  Ahora puedes click derecho en una carpeta y seleccionar 'Abrir MeshRepair CLI aqui'" -ForegroundColor Cyan
}
catch {
    Write-Host "Error al registrar en el Registry: $PSItem" -ForegroundColor Red
    exit 1
}
