# MeshRepair v2.1.0 - Feature Summary

## 🎉 **Nuevas Características Implementadas**

### 1️⃣ **Modo Interactivo Mejorado (CLI)**
- ✅ **Enter vacío = carpeta actual**: Presiona Enter en la primera pregunta para usar el directorio actual
- ✅ **Flag `-d` para defaults**: 
  - `meshrepair` → `-d` → procesa carpeta actual con opciones por defecto
  - `meshrepair` → `archivo.stl -d` → procesa archivo específico con defaults
  - `meshrepair -d` → modo rápido desde línea de comandos
- ✅ **Flujo super rápido**: `meshrepair` + Enter + Enter = procesamiento instantáneo

### 2️⃣ **Exportación a STL**
- ✅ Parámetro `--outputFormat stl` para convertir modelos reparados de vuelta a STL
- ✅ Usa PrusaSlicer para la conversión 3MF → STL
- ✅ Elimina archivos 3MF intermedios automáticamente si se elige STL

### 3️⃣ **Interfaz Gráfica (GUI) - NUEVO! 🎨**
- ✅ **Diseño moderno** con ModernWpfUI (estilo Windows 11)
- ✅ **Drag & Drop**: Arrastra archivos/carpetas directamente
- ✅ **Diálogos nativos**: Browse para archivos y carpetas
- ✅ **Controles intuitivos**:
  - Input path con drag & drop
  - Output path opcional
  - Checkbox: Convertir a STL
  - Checkbox: Clonar jerarquía
  - NumberBox: Timeout configurable
- ✅ **Progreso en tiempo real**: Muestra el output del proceso de reparación
- ✅ **Progress bar**: Indicador visual durante el procesamiento

### 4️⃣ **Mejoras de UI/UX**
- ✅ **Progreso de batch**: Muestra `[X/Total]` al procesar múltiples archivos
- ✅ **Rutas limpias**: Solo muestra nombres de archivo en lugar de paths completos
- ✅ **Mensajes claros**: Output más legible y organizado

### 5️⃣ **Fixes de Bugs**
- ✅ Fixed crash cuando `--outputFilePath` no se especifica
- ✅ Archivos de salida ahora se guardan en la misma carpeta (no crea subcarpeta "MeshRepair")
- ✅ Validación mejorada de rutas y parámetros

---

## 📦 **Estructura del Proyecto**

```
MeshRepair/
├── MeshRepair/                    # Proyecto CLI (consola)
│   ├── Program.cs                 # Lógica principal (ahora público para ser usado por GUI)
│   ├── PrusaSlicer/              # Binarios de PrusaSlicer embebidos
│   └── ...
│
├── MeshRepairGUI/                 # Proyecto GUI (interfaz gráfica) - NUEVO
│   ├── MainWindow.xaml           # Diseño de la interfaz
│   ├── MainWindow.xaml.cs        # Lógica de la interfaz
│   ├── App.xaml                  # Configuración de la aplicación
│   └── ...
│
├── installer/                     # Configuración de Inno Setup
│   └── setup.iss                 # Script para crear instalador
│
├── chocolatey/                    # Paquete Chocolatey
│   ├── meshrepair.nuspec         # Manifest de Chocolatey
│   └── tools/
│       ├── chocolateyinstall.ps1
│       └── chocolateyuninstall.ps1
│
├── build-release.ps1             # Script automatizado para crear releases
├── DISTRIBUTION.md               # Guía de distribución
└── README-NEW.md                 # README actualizado
```

---

## 🚀 **Uso**

### **CLI (Línea de Comandos)**

#### Modo Interactivo:
```powershell
meshrepair
# [Enter] - Usa carpeta actual
# [Enter] - Misma ubicación de salida
# N - Mantener como 3MF
# Y - Clonar jerarquía
# [Enter] - Timeout por defecto
```

#### Modo Rápido con Defaults:
```powershell
# Procesar carpeta actual
meshrepair -d

# Procesar archivo específico con defaults
meshrepair
archivo.stl -d
```

#### Línea de Comandos Completa:
```powershell
# Reparar y convertir a STL
meshrepair --inputFilePath "C:\Models" --outputFormat stl

# Con todas las opciones
meshrepair --inputFilePath "C:\Input" `
           --outputFilePath "C:\Output" `
           --outputFormat stl `
           --cloneFolderHierarchy true `
           --timeoutSeconds 600
```

### **GUI (Interfaz Gráfica)**

1. Ejecutar `MeshRepair-GUI.exe`
2. **Arrastrar** carpeta/archivo al campo "Input Path" O usar botón "Browse"
3. (Opcional) Especificar carpeta de salida
4. Configurar opciones:
   - ☑ Convert to STL after repair
   - ☑ Clone folder hierarchy
   - ⏱ Timeout per file: 600s
5. Click en **"Start Repair"**
6. Ver progreso en tiempo real en el panel inferior

---

## 📋 **Comandos de Build**

### Compilar todo:
```powershell
dotnet build MeshRepair.sln --configuration Release
```

### Compilar solo CLI:
```powershell
dotnet build MeshRepair\MeshRepair.csproj --configuration Release
```

### Compilar solo GUI:
```powershell
dotnet build MeshRepairGUI\MeshRepairGUI.csproj --configuration Release
```

### Crear Release completo:
```powershell
.\build-release.ps1 -Version "2.1.0"
```

### Ejecutar desde código (testing):
```powershell
# CLI
cd MeshRepair
dotnet run

# GUI
cd MeshRepairGUI
dotnet run
```

---

## 🎨 **Capturas de la GUI**

La interfaz gráfica incluye:
- Header con título y descripción
- Sección de Input/Output con drag & drop
- Opciones configurables (checkboxes, numberbox)
- Panel de progreso con scroll
- Progress bar animada durante procesamiento
- Botones de acción (Clear, Start Repair)

**Tema**: Sigue automáticamente el tema del sistema (Light/Dark mode)

---

## 📦 **Distribución**

### Paquetes disponibles:

1. **ZIP Portable** (`MeshRepair-v2.1.0-win-x64.zip`)
   - Incluye CLI + GUI
   - No requiere instalación
   
2. **Instalador EXE** (Inno Setup)
   - Instalador gráfico
   - Opción para agregar al PATH
   - Crear accesos directos
   
3. **Chocolatey Package**
   ```powershell
   choco install meshrepair
   ```
   
4. **winget Package**
   ```powershell
   winget install SynrgStudio.MeshRepair
   ```

Ver [DISTRIBUTION.md](DISTRIBUTION.md) para instrucciones detalladas.

---

## 🔧 **Tecnologías Usadas**

### CLI:
- .NET 8.0
- Windows 3D Printing API
- PrusaSlicer (embedded)

### GUI:
- WPF (Windows Presentation Foundation)
- ModernWpfUI (Windows 11 Fluent Design)
- .NET 8.0

---

## 📝 **Changelog v2.1.0**

### Added:
- ✨ Interfaz gráfica (GUI) con WPF + ModernWpfUI
- ✨ Drag & drop support en GUI
- ✨ Modo interactivo mejorado con Enter vacío = carpeta actual
- ✨ Flag `-d` para usar defaults rápidamente
- ✨ Parámetro `--outputFormat` para exportar a STL
- ✨ Progreso `[X/Total]` en procesamiento batch
- ✨ Scripts automatizados de build y release

### Changed:
- 🔄 Archivos de salida ahora se guardan en la misma carpeta por defecto
- 🔄 Output de consola más limpio (solo nombres de archivo)
- 🔄 Clase `Program` ahora es pública (compartida entre CLI y GUI)

### Fixed:
- 🐛 Crash cuando `--outputFilePath` está vacío
- 🐛 Validación mejorada de paths

---

## 🎯 **Próximos Pasos**

- [ ] Crear releases en GitHub
- [ ] Publicar paquete Chocolatey
- [ ] Crear manifest winget
- [ ] Agregar iconos personalizados
- [ ] (Opcional) Temas personalizados en GUI
- [ ] (Opcional) Guardado de configuración por defecto
- [ ] (Opcional) Historial de archivos procesados

---

**¡Disfruta de MeshRepair v2.1.0!** 🎉
