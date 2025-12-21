$ErrorActionPreference = 'Stop'

$packageName = 'meshrepair'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url64 = 'https://github.com/SynrgStudio/MeshRepair/releases/download/v3.1.0/MeshRepair-Setup-v3.1.0.exe'
$checksum64 = 'ACA26010DBB4C559D0AE4B4FB6D09AAD9BCAAE497D2170E7BC8F39EC3FADA05F'

$packageArgs = @{
  packageName    = $packageName
  unzipLocation  = $toolsDir
  fileType       = 'exe'
  url64bit       = $url64
  checksum64     = $checksum64
  checksumType64 = 'sha256'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
  validExitCodes = @(0)
  softwareName   = 'MeshRepair'
}

Install-ChocolateyPackage @packageArgs
