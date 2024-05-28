# Windows 3MF Repair Commandline Interface

## About
This is a very simple CLI tool used to repair 3MF files, presumably using the same functionality as Windows 3D Builder. I wrote this to be used in conjunction with other automation, so all this does is take an input file and repair it.

## Usage
See releases for latest compiled .exe

  ``` --inputFilePath "C:/Folder/model.3mf" ```
  Repair and overwrite the original model

  ``` --outputFilePath "C:/Folder/model-repaired.3mf" ```
  Optional: Specifying an outputFilePath will save the repair to a new file instead

  ``` --timeoutSeconds 600 ```
  Optional: How long to attempt to repair the model for before cancelling, large models or ones with many issues could take a while. Default is 10 minutes.

  ``` --help ```
  To see the usage again

# Automation
For further automation, I've uploaded a powershell script that utilises PrusaSlicer in order to center and export models as .3MFs, followed by this repair process. See ConvertSTL.ps1.
Simply edit this file to adjust the paths to your prusa-slicer.exe and MeshRepair.exe. 

## Requirements
- Powershell (any Windows machine will have this)
- PrusaSlicer (Slic3r or anything based on Slic3r _could_ work too)


## Usage
ConvertSTL.ps1 will find, convert, and then repair .STL files using this CLI tool. Simply edit the ps1 file in notepad or your editor of choice and add the paths to prusa-slicer.exe, and MeshRepair.exe

To run it, open terminal/powershell and navigate to the containing directory

``` cd "C:/folder-containing-ConvertSTL.ps1" ```

Then, run the tool, passing in a folder that contains STL files

``` ./ConvertSTL.ps1 "C:/folder-containing-STLs" ```
