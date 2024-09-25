# MeshRepair: A 3D Printing Model Repair CLI Tool
![image](https://github.com/user-attachments/assets/b9d09525-5179-48d0-8ccb-0834e25a23ce)

## About

This is a simple command-line tool used to repair 3D assets that have issues with 3D printing. It converts input files to the `.3mf` format, checks for issues, and attmpts to repair any that are found, utilizing (I assume) the same repair capabilities as Windows 3D Builder.

For easy conversion of assets into `.3mf`, a portable, stripped-down version of PrusaSlicer is included.

## Usage

See the [Releases]([#"../../../../releases) section for the latest compiled `.exe`.

### Arguments
Call the .exe with arguments using your preferred CLI

- `--inputFilePath`

  **Required**. Specify a file or folder to repair. If a folder is specified, all compatible files within the folder (and its subfolders) will be repaired.

- `--outputFilePath`

  **Optional**. Specify a directory where the repaired files will be saved. If not specified, the repaired files will be placed next to the originals.

- `--cloneFolderHierarchy [true|false]`

  **Optional**. Default: `true`. If set to `true` and a folder of models is selected, the entire folder hierarchy will be cloned in the output directory for tidiness. Set to `false` to place repaired files directly in the output directory (or next to the originals if `--outputFilePath` is not specified).

- `--timeoutSeconds [number]`

  **Optional**. Specify how long (in seconds) to attempt to repair the model before cancelling. Large models or those with many issues may take a while. Default is `600` seconds (10 minutes).

- `--help`

  Displays the help message.

### Supported File Types

Any file type that is compatible with PrusaSlicer. Supported formats include:
```.stl, .step, .stp, .3mf, .obj, .amf```

### Examples

**Repair a single file:**

```MeshRepair.exe --inputFilePath "C:/Folder/model.stl"```

**Repair a single file and specify an output directory:**

```MeshRepair.exe --inputFilePath "C:/Folder/model.stl" --outputFilePath "C:/Folder/Repaired"```

**Scan a folder for assets and repair all files (default behavior clones folder hierarchy):**

```MeshRepair.exe --inputFilePath "C:/Folder/Models"```

**Scan a folder for assets and replicate the folder hierarchy under the output folder:**

```MeshRepair.exe --inputFilePath "C:/Folder/Models" --outputFilePath "C:/FixedModels"```


**Scan a folder for assets and place the fixed files beside the originals without cloning the folder hierarchy:**

```MeshRepair.exe --inputFilePath "C:/Folder/Models" --cloneFolderHierarchy false```
