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

  ## Automation
  For further automation, I've uploaded a powershell script that utilises PrusaSlicer in order to center and export models as .3MFs, followed by this repair process.
  WIP Link
