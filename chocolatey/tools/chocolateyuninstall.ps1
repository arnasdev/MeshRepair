$ErrorActionPreference = 'Stop'

$packageArgs = @{
  softwareName   = 'MeshRepair'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
  validExitCodes = @(0)
}

Uninstall-ChocolateyPackage @packageArgs
