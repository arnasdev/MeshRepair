using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Printing3D;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MeshRepairCLI
{
    class Program
    {
        static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".stl", ".step", ".stp", ".3mf", ".obj", ".amf"
        };
        static readonly string InputFilePathArg = "inputFilePath";
        static readonly string OutputFilePathArg = "outputFilePath";
        static readonly string TimeoutSecondsArg = "timeoutSeconds";
        static readonly string CloneFolderHierarchyArg = "cloneFolderHierarchy";
        static readonly string HelpArg = "help";

        static string inputFilePath = "";
        static string outputFilePath = "";
        static int timeoutSeconds = 60 * 10;
        static bool cloneFolderHierarchy = true;
        static bool isDirectory = false;

        static async Task Main(string[] args)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                PrintColored("Press any key after attaching debugger to continue...\n", ConsoleColor.Magenta);
                Console.ReadKey();
            }
#endif
            PrintHeader();
            var arguments = ParseArguments(args);
            if (!ValidateArguments(arguments))
            {
                return;
            }

            // Fix any path weirdness
            inputFilePath = Path.GetFullPath(inputFilePath);
            outputFilePath = Path.GetFullPath(outputFilePath);

            string[] paths;
            List<string> failedRepairs = new List<string>();

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                if (IsDirectory(inputFilePath))
                {
                    // Recursively search the directory for these files and construct a string array of all of their paths
                    paths = Directory.GetFiles(inputFilePath, "*.*", SearchOption.AllDirectories)
                        .Where(file => SupportedExtensions.Contains(Path.GetExtension(file)))
                        .ToArray();

                    isDirectory = true;

                    PrintColored($"Found {paths.Count()} files for conversion/repair.", ConsoleColor.Yellow);
                }
                else
                {
                    // Convert a single .3mf
                    if (!SupportedExtensions.Contains(Path.GetExtension(inputFilePath)))
                    {
                        PrintColored("Input file format not supported for conversion to 3mf", ConsoleColor.Red);
                        return;
                    }
                    paths = new string[] { inputFilePath };
                }

                foreach(string file in paths)
                {
                    PrintColored($"\n{file}", ConsoleColor.Yellow);

                    // Construct the desired file path
                    string convertedFilePath;
                    if (cloneFolderHierarchy && isDirectory)
                    {   
                        // Copy the folder structure of the found file relative to the root (inputFilePath) for tidiness
                        string relativePath = Path.GetRelativePath(inputFilePath, file);
                        relativePath = Path.ChangeExtension(relativePath, ".3mf");

                        if (outputFilePath != String.Empty)
                        {
                            convertedFilePath = Path.Combine(outputFilePath, relativePath);
                        }
                        else
                        {
                            convertedFilePath = Path.Combine(inputFilePath, "MeshRepair", relativePath);
                        }
                    }
                    else
                    {
                        if (isDirectory)
                        {
                            convertedFilePath = Path.ChangeExtension(file, ".3mf");
                        }
                        else
                        {
                            convertedFilePath = Path.ChangeExtension(inputFilePath, ".3mf");
                        }
                        
                        if (outputFilePath != String.Empty)
                        {
                            string fileName = Path.GetFileName(convertedFilePath);
                            convertedFilePath = Path.Combine(outputFilePath, fileName);
                        }
                    }

                    string dir = Path.GetDirectoryName(convertedFilePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // Conversion
                    if (!Path.GetExtension(file).Equals(".3mf", StringComparison.OrdinalIgnoreCase) || outputFilePath != string.Empty)
                    {
                        if (Path.GetExtension(file).Equals(".3mf")) // messy, but works
                        {
                            PrintColored("\tInput file is already a 3mf file, copying to specified output path", ConsoleColor.Yellow);
                        }
                        bool success = await ConvertTo3MF(file, convertedFilePath);
                        if (!success)
                        {
                            failedRepairs.Add(convertedFilePath);
                            continue;
                        }
                    }
                    else
                    {
                        PrintColored("\tInput file is already a 3mf file, proceeding to repair step.", ConsoleColor.Yellow);
                        convertedFilePath = file;
                    }
                    
                    // Repair
                    if (File.Exists(convertedFilePath))
                    {
                        bool success = await TryRepairFile(convertedFilePath);
                        if (!success)
                        {
                            failedRepairs.Add(convertedFilePath);
                        }
                    }
                    else
                    {
                        PrintColored($"\tCan't find file {convertedFilePath}", ConsoleColor.Red);
                        failedRepairs.Add(convertedFilePath);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
                PrintColored("Fatal error, exiting.", ConsoleColor.Red);
                return;
            }

            sw.Stop();
            PrintColored($"\n{sw.Elapsed.Seconds} seconds elapsed", ConsoleColor.Green);
            PrintColored($"{paths.Count() - failedRepairs.Count}/{paths.Count()} files verified/repaired", ConsoleColor.Green);
            foreach(var failed in failedRepairs)
            {
                PrintColored($"\t{failed}", ConsoleColor.Red);
            }
        }

        #region Conversion
        static async Task<bool> ConvertTo3MF(string inputFilePath, string outputFilePath)
        {
            // Get the base directory of the application
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Construct the path to PrusaSlicer.exe
            string prusaSlicerPath = Path.Combine(exeDirectory, "PrusaSlicer", "prusa-slicer.exe");

            // Check if the PrusaSlicer executable exists
            if (!File.Exists(prusaSlicerPath))
            {
                Console.WriteLine($"\tPrusaSlicer not found at {prusaSlicerPath}");
                return false;
            }

            // Construct the arguments to pass to the executable
            string arguments = string.Join(" ", new string[]
            {
                "--export-3mf",
                "--center",
                "0,0",
                "-o",
                $"\"{outputFilePath}\"",
                $"\"{inputFilePath}\""
            });

            // Set up the process start information
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = prusaSlicerPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Capture the output and error streams
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            PrintColored($"\t{e.Data}", ConsoleColor.Yellow);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            PrintColored($"\tError: {e.Data}", ConsoleColor.Red);
                        }
                    };

                    // Start the process
                    process.Start();

                    // Begin reading the output streams
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for the process to exit
                    await process.WaitForExitAsync();

                    // Check the exit code to determine success
                    if (process.ExitCode == 0)
                    {
                        PrintColored("\tConversion successful.", ConsoleColor.Yellow);
                        return true;
                    }
                    else
                    {
                        PrintColored($"\tProcess exited with code {process.ExitCode}", ConsoleColor.Red);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintColored($"\tAn error occurred: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }
        #endregion

        #region Mesh Repair
        static async Task<bool> TryRepairFile(string inputFilePath)
        {
            try
            {
                // Create a blank 3MF package in memory
                Printing3D3MFPackage package = new Printing3D3MFPackage();

                // Read the input file
                StorageFile inputFile = await StorageFile.GetFileFromPathAsync(inputFilePath);

                // Load the input file model into the 3MF package
                Printing3DModel model;
                using (IRandomAccessStream inputStream = await inputFile.OpenAsync(FileAccessMode.Read))
                {
                    model = await package.LoadModelFromPackageAsync(inputStream);
                }

                // If the model can be verified, don't repair
                bool preRepairVerifySuccess = await VerifyMeshes(model);
                if (preRepairVerifySuccess)
                {
                    PrintColored("\tMesh verified with no errors, skipping repair", ConsoleColor.Green);
                    return true;
                }
                else
                {
                    PrintColored("\tFound errors - proceeding with repair", ConsoleColor.Red);
                }

                // Attempt repair with timeout
                bool repairSuccess = await RepairWithTimeoutAsync(model, TimeSpan.FromSeconds(timeoutSeconds));

                // Verify the fix
                bool postRepairVerifySuccess = await VerifyMeshes(model);
                if (postRepairVerifySuccess)
                {
                    PrintColored("\tVerification successful", ConsoleColor.Green);
                }
                else
                {
                    PrintColored("\tFailed to verify mesh, exiting without saving", ConsoleColor.Red);
                    return false;
                }

                // Save the repaired model back into the 3MF package
                await package.SaveModelToPackageAsync(model);
                // Changes don't seem to apply unless we save a seperate file - so save seperately and then overwrite the original.
                await Overwrite3MF(package, inputFile, inputFilePath);

                return true;
            }
            catch (Exception ex)
            {
                PrintColored($"\tError: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        static async Task<bool> RepairWithTimeoutAsync(Printing3DModel model, TimeSpan timeout)
        {
            using(var cts = new CancellationTokenSource())
            {
                var repairTask = model.RepairAsync().AsTask(cts.Token);
                var delayTask = Task.Delay(timeout, cts.Token);

                var completedTask = await Task.WhenAny(repairTask, delayTask);
                if(completedTask == repairTask)
                {
                    cts.Cancel();

                    await repairTask;

                    if(repairTask.Status == TaskStatus.RanToCompletion)
                    {
                        PrintColored("\tFinished repair.", ConsoleColor.Green);
                        return true;
                    }
                    else
                    {
                        PrintColored("\tFailed repair.", ConsoleColor.Red);
                        return false;
                    }
                }
                else
                {
                    cts.Cancel();

                    PrintColored("\tRepair exceeded timeout, cancelled", ConsoleColor.Red);
                    return false;
                }
            }
        }

        static async Task<bool> VerifyMeshes(Printing3DModel model)
        {
            bool isValid = true;
            foreach(var component in model.Components)
            {
                Printing3DMeshVerificationResult result = await component.Mesh.VerifyAsync(Printing3DMeshVerificationMode.FindAllErrors);
                if (!result.IsValid)
                {
                    isValid = false;
                }
            }
            return isValid;
        }
        #endregion

        #region Saving
        // TODO: clean this up, Distinction was made in older code between overwriting a 3FM or saving it seperately. 
        // We save a temp file, delete the original, then move the temp in its place.
        static async Task Overwrite3MF(Printing3D3MFPackage package, StorageFile originalFile, string saveFilePath)
        {
            string folder = Path.GetDirectoryName(saveFilePath);
            string filename = "temp-"+Path.GetFileName(saveFilePath);
            string temp = Path.Combine(folder, filename);

         
            await SaveSeperate3MF(package, temp);

            StorageFile outputFile = await StorageFile.GetFileFromPathAsync(temp);

            // Delete the original file
            await originalFile.DeleteAsync();

            // Rename the new file to the original
            await outputFile.RenameAsync(System.IO.Path.GetFileName(saveFilePath));
        }

        
        static async Task SaveSeperate3MF(Printing3D3MFPackage package, string path)
        {
            IRandomAccessStream saveStream = await package.SaveAsync();
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(path));
            StorageFile outputFile = await storageFolder.CreateFileAsync(System.IO.Path.GetFileName(path), CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream outputStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAsync(saveStream, outputStream);
            }
            saveStream.Dispose();
        }
        #endregion

        #region Arg Parsing
        static bool ValidateArguments(Dictionary<string, string> arguments)
        {
            if (arguments.Count == 0)
            {
                ShowHelp();
                return false;
            }

            if (arguments.ContainsKey(HelpArg))
            {
                ShowHelp();
                return false;
            }

            if (!arguments.ContainsKey(InputFilePathArg))
            {
                ShowHelp();
                return false;
            }

            cloneFolderHierarchy = GetArgumentValue(arguments, CloneFolderHierarchyArg, true);
            inputFilePath = GetArgumentValue(arguments, InputFilePathArg);
            outputFilePath = GetArgumentValue(arguments, OutputFilePathArg);
            timeoutSeconds = GetArgumentValue(arguments, TimeoutSecondsArg, timeoutSeconds);

            return true;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"--{InputFilePathArg}=<input_file_path>     Specify the path to an individual model or a folder of models to repair.");
            Console.WriteLine($"--{CloneFolderHierarchyArg}=<true>         Optional: If a folder of models is specified, clone the folder hierarchy with the repaired files. Default is true.");
            Console.WriteLine($"--{OutputFilePathArg}=<output_file_path>   Optional: Specify a folder to put the repaired files.");
            Console.WriteLine($"--{TimeoutSecondsArg}=<60>                 Optional: Specify how long to repair a model before giving up. Default is 60 seconds.");
            Console.WriteLine("--help                                      Show help information.");
        }

        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, string>();
            string currentKey = null;
            StringBuilder currentValue = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("--"))
                {
                    // If we're processing a previous key, save its value before moving on
                    if (currentKey != null)
                    {
                        arguments[currentKey] = currentValue.ToString().Trim();
                        currentValue.Clear();
                    }

                    // Start processing a new key
                    currentKey = arg.Substring(2); // Remove the '--' prefix
                }
                else
                {
                    if (currentKey == null)
                    {
                        // Handle the case where a value is provided without a key
                        throw new ArgumentException($"Value '{arg}' provided without a corresponding key.");
                    }

                    // Append the argument to the current value
                    if (currentValue.Length > 0)
                    {
                        currentValue.Append(' ');
                    }
                    currentValue.Append(arg);
                }
            }

            // Add the last key-value pair after the loop ends
            if (currentKey != null)
            {
                arguments[currentKey] = currentValue.ToString().Trim();
            }

            return arguments;
        }

        static string GetArgumentValue(Dictionary<string, string> arguments, string key, string defaultValue = "")
        {
            return arguments.ContainsKey(key) ? arguments[key] : defaultValue;
        }

        static bool GetArgumentValue(Dictionary<string, string> arguments, string key, bool defaultValue)
        {
            if (arguments.ContainsKey(key) && arguments[key] != null)
            {
                return bool.Parse(arguments[key]);
            }
            return defaultValue;
        }

        static int GetArgumentValue(Dictionary<string, string> arguments, string key, int defaultValue)
        {
            if (arguments.ContainsKey(key) && arguments[key] != null)
            {
                return int.Parse(arguments[key]);
            }
            return defaultValue;
        }

        static float GetArgumentValue(Dictionary<string, string> arguments, string key, float defaultValue)
        {
            if (arguments.ContainsKey(key) && arguments[key] != null)
            {
                return float.Parse(arguments[key]);
            }
            return defaultValue;
        }
        #endregion

        #region Helpers
        static bool IsDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is null or empty", nameof(path));
            }
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException("The specified path does not exist.", path);
            }

            System.IO.FileAttributes attr = File.GetAttributes(path);
            return attr.HasFlag(System.IO.FileAttributes.Directory);
        }

        static void PrintColored(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        static void PrintHeader()
        {
            PrintColored(@"-------------------------------------------------------------------
MeshRepair v1.0.0 - https://github.com/arnasdev/Windows3MFRepairCLI
-------------------------------------------------------------------", ConsoleColor.Blue);
        }
        #endregion
    }
}