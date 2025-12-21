# MeshRepair: A 3D Printing Model Repair CLI Tool

![MeshRepair Banner](https://github.com/user-attachments/assets/0e78e5d5-c72c-4106-a7ec-85e1ffc09ae5)

[![GitHub release](https://img.shields.io/github/v/release/SynrgStudio/MeshRepair)](https://github.com/SynrgStudio/MeshRepair/releases)
[![License](https://img.shields.io/github/license/SynrgStudio/MeshRepair)](LICENSE)

## ✨ Features

- **🎮 Interactive Mode**: User-friendly guided experience
- **📄 STL Export**: Convert repaired models back to STL format
- **📊 Progress Tracking**: Visual progress counter for batch processing  
- **⚡ Quick Defaults**: Use `-d` flag for instant processing
- **📁 Batch Processing**: Repair entire folders with hierarchy preservation
- **🔧 Multiple Formats**: Supports STL, STEP, 3MF, OBJ, AMF

## 📦 Installation

### Option 1: Download Portable ZIP
Download the latest release from the [Releases](https://github.com/SynrgStudio/MeshRepair/releases) page.

### Option 2: Chocolatey (Recommended for developers)
```powershell
choco install meshrepair
```

### Option 3: Installer (EXE)
Download and run `MeshRepair-Setup-vX.X.X.exe` from [Releases](https://github.com/SynrgStudio/MeshRepair/releases).
- Optionally adds to system PATH during installation

### Option 4: winget
```powershell
winget install SynrgStudio.MeshRepair
```

## 🚀 Usage

### Interactive Mode (Easiest!)
Simply run without arguments:
```powershell
meshrepair
```

Then follow the prompts:
- Press **Enter** at first prompt to use current directory
- Add **`-d`** for quick defaults (e.g., just type `-d` and Enter)

### Command Line Examples

**Repair current directory with defaults:**
```powershell
meshrepair -d
```

**Repair a single file:**
```powershell
meshrepair --inputFilePath "C:/Folder/model.stl"
```

**Repair and convert to STL:**
```powershell
meshrepair --inputFilePath "C:/Models" --outputFormat stl
```

**Repair folder with custom output:**
```powershell
meshrepair --inputFilePath "C:/Input" --outputFilePath "C:/Output"
```

**All options:**
```powershell
meshrepair --inputFilePath "C:/Input" `
           --outputFilePath "C:/Output" `
           --outputFormat stl `
           --cloneFolderHierarchy true `
           --timeoutSeconds 600
```

## 📖 Arguments

| Argument | Description | Default |
|----------|-------------|---------|
| `--inputFilePath` | Path to file or folder to repair | *Required* |
| `--outputFilePath` | Output directory (optional) | Same as input |
| `--outputFormat` | Output format: `3mf` or `stl` | `3mf` |
| `--cloneFolderHierarchy` | Preserve folder structure | `true` |
| `--timeoutSeconds` | Repair timeout per file | `600` |
| `-d` | Use default settings | - |
| `--help` | Show help information | - |

## 🎯 Interactive Mode Features

### Quick Shortcuts:
- **Enter** on first prompt = Use current directory
- **`-d`** = Use all defaults
- **`filename.stl -d`** = Repair specific file with defaults

### Example Flow:
```
Input Folder or File (press Enter for current directory, or add '-d' for defaults): 
[Press Enter]
Using current directory: D:\Models
Using default settings for all options.

[1/15] model.stl
        Conversion successful.
        Found errors - proceeding with repair
        Finished repair.
        Verification successful
...
```

## 🔧 Supported File Types

- `.stl` - Stereolithography
- `.3mf` - 3D Manufacturing Format  
- `.obj` - Wavefront OBJ
- `.step` / `.stp` - STEP CAD format
- `.amf` - Additive Manufacturing Format

## 🏗️ How It Works

1. **Conversion**: Input files are converted to `.3mf` format using PrusaSlicer
2. **Verification**: Models are checked for errors using Windows 3D Printing API
3. **Repair**: If errors found, automatic repair is attempted
4. **Export**: Optionally convert back to STL or keep as 3MF

> **Note**: This tool utilizes the same repair capabilities as Windows 3D Builder, leveraging the Windows 3D Printing API.

## 🛠️ Building from Source

### Prerequisites
- .NET 8.0 SDK
- Windows 10/11

### Build
```powershell
git clone https://github.com/SynrgStudio/MeshRepair.git
cd MeshRepair
dotnet build MeshRepair.sln --configuration Release
```

### Create Release Package
```powershell
.\build-release.ps1 -Version "2.1.0"
```

See [DISTRIBUTION.md](DISTRIBUTION.md) for creating installers and packages.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 🙏 Credits

- Uses a portable version of [PrusaSlicer](https://github.com/prusa3d/PrusaSlicer) for format conversion
- Leverages Windows 3D Printing API for mesh repair

## 📬 Support

- 🐛 [Report Issues](https://github.com/SynrgStudio/MeshRepair/issues)
- 💬 [Discussions](https://github.com/SynrgStudio/MeshRepair/discussions)

---

Made with ❤️ by [SynrgStudio](https://github.com/SynrgStudio)
