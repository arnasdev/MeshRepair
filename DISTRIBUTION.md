# MeshRepair - Distribution Package Creation

Este directorio contiene los archivos necesarios para crear paquetes de distribución de MeshRepair.

## 📦 Inno Setup (Instalador EXE para Windows)

### Prerrequisitos
1. Descargar e instalar [Inno Setup](https://jrsoftware.org/isdl.php)
2. Compilar el proyecto en modo Release

### Pasos para crear el instalador

```powershell
# 1. Compilar el proyecto
cd D:\MeshRepairer\MeshRepair
dotnet publish MeshRepair.sln `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output MeshRepair\bin\Release\net8.0-windows10.0.19041.0\win-x64

# 2. Abrir Inno Setup y compilar
# - Abre installer\setup.iss con Inno Setup
# - Click en "Build" → "Compile"
# - El instalador se generará en releases\MeshRepair-Setup-v2.1.0.exe
```

### Personalización
- **Icono**: Si tienes un icono, colócalo como `icon.ico` en la raíz y descomenta la línea `SetupIconFile` en `setup.iss`
- **Versión**: Edita `#define MyAppVersion "2.1.0"` en `setup.iss`

---

## 🍫 Chocolatey (Package Manager)

### Prerrequisitos
1. Instalar Chocolatey: https://chocolatey.org/install
2. Tener el ZIP de release creado y subido a GitHub

### Pasos para crear el paquete

```powershell
# 1. Crear el release ZIP primero
cd D:\MeshRepairer\MeshRepair
.\build-release.ps1  # O el script que uses para crear el ZIP

# 2. Subir el ZIP a GitHub Releases
# - Ve a https://github.com/SynrgStudio/MeshRepair/releases
# - Create new release con tag v2.1.0
# - Sube MeshRepair-v2.1.0-win-x64.zip

# 3. Calcular el checksum del ZIP
$zip = "releases\MeshRepair-v2.1.0-win-x64.zip"
(Get-FileHash $zip -Algorithm SHA256).Hash

# 4. Actualizar el checksum en chocolatey\tools\chocolateyinstall.ps1
# Reemplaza 'REPLACE_WITH_ACTUAL_CHECKSUM' con el hash calculado

# 5. Empaquetar con Chocolatey
cd chocolatey
choco pack

# 6. (Opcional) Probar localmente antes de publicar
choco install meshrepair -s . -y

# 7. Publicar a Chocolatey
choco push meshrepair.2.1.0.nupkg --source https://push.chocolatey.org/
```

### Notas importantes
- **API Key**: Necesitas una API key de Chocolatey (obtener en https://community.chocolatey.org/account)
- **Primera publicación**: La primera vez puede tardar en ser aprobada (revisión manual)
- **Actualizaciones**: Las actualizaciones son más rápidas (aprobación automática si no hay problemas)

---

## 🪟 winget (Windows Package Manager)

### Pasos para crear el manifest

```powershell
# 1. Instalar winget-create
winget install wingetcreate

# 2. Crear manifest automáticamente
wingetcreate new https://github.com/SynrgStudio/MeshRepair/releases/download/v2.1.0/MeshRepair-v2.1.0-win-x64.zip

# 3. Editar el manifest generado si es necesario
# Los archivos se generarán en manifests\

# 4. Fork del repo de winget-pkgs
# https://github.com/microsoft/winget-pkgs

# 5. Crear PR con los manifests
# - Copiar los archivos del manifest a tu fork
# - Crear Pull Request al repo oficial
```

### Estructura del manifest winget
Los archivos se deben colocar en:
```
manifests/s/SynrgStudio/MeshRepair/2.1.0/
├── SynrgStudio.MeshRepair.installer.yaml
├── SynrgStudio.MeshRepair.locale.en-US.yaml
└── SynrgStudio.MeshRepair.yaml
```

---

## 🚀 Script automatizado (TODO)

Crear un script `build-all-releases.ps1` que:
1. Compile el proyecto
2. Cree el ZIP de release
3. Genere el instalador Inno Setup
4. Prepare el paquete Chocolatey
5. Genere los manifests winget
6. Calcule checksums automáticamente

---

## 📝 Checklist de Release

- [ ] Actualizar versión en `Program.cs` (header)
- [ ] Actualizar versión en `setup.iss`
- [ ] Actualizar versión en `meshrepair.nuspec`
- [ ] Compilar en Release mode
- [ ] Crear ZIP de release
- [ ] Crear instalador Inno Setup
- [ ] Subir a GitHub Releases
- [ ] Calcular checksum del ZIP
- [ ] Actualizar checksum en Chocolatey
- [ ] Crear paquete Chocolatey
- [ ] Publicar en Chocolatey
- [ ] Crear manifest winget
- [ ] PR a winget-pkgs

