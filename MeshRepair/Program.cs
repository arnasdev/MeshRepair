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
    public class Program
    {
        static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".stl", ".step", ".stp", ".3mf", ".obj", ".amf"
        };
        static readonly string InputFilePathArg = "inputFilePath";
        static readonly string OutputFilePathArg = "outputFilePath";
        static readonly string TimeoutSecondsArg = "timeoutSeconds";
        static readonly string CloneFolderHierarchyArg = "cloneFolderHierarchy";
        static readonly string OutputFormatArg = "outputFormat";
        static readonly string HelpArg = "help";

        static string inputFilePath = "";
        static string outputFilePath = "";
        static int timeoutSeconds = 60 * 10;
        static bool cloneFolderHierarchy = true;
        static bool isDirectory = false;
        static string outputFormat = "3mf";
        static bool useDefaults = false;

        // Progress tracking
        static int currentFileIndex = 0;
        static int totalFilesCount = 0;
        static string currentFileName = "";

        public static async Task Main(string[] args)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                PrintColored("Press any key after attaching debugger to continue...\n", ConsoleColor.Magenta);
                Console.ReadKey();
            }
#endif
            PrintHeader();
            
            Dictionary<string, string> arguments;
            
            // Check for -d flag (use defaults)
            if (args.Length > 0 && args.Any(arg => arg.Equals("-d", StringComparison.OrdinalIgnoreCase)))
            {
                useDefaults = true;
                // Remove -d from args
                args = args.Where(arg => !arg.Equals("-d", StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            
            // If no arguments provided (or only -d was provided), enter interactive mode
            if (args.Length == 0)
            {
                arguments = InteractiveMode();
                if (arguments == null)
                {
                    return;
                }
            }
            else
            {
                arguments = ParseArguments(args);
            }
            
            if (!ValidateArguments(arguments))
            {
                return;
            }

            // Fix any path weirdness
            inputFilePath = Path.GetFullPath(inputFilePath);
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                outputFilePath = Path.GetFullPath(outputFilePath);
            }

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

                totalFilesCount = paths.Length;
                currentFileIndex = 0;

                foreach(string file in paths)
                {
                    currentFileIndex++;
                    currentFileName = isDirectory ? Path.GetFileName(file) : Path.GetFileName(file); // Keep simple for now
                    
                    UpdateStatus("Iniciando...");

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
                            // Keep in the same folder structure, just change extension
                            convertedFilePath = Path.Combine(inputFilePath, relativePath);
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
                            UpdateStatus("El archivo ya es 3mf, copiando...");
                        }
                        UpdateStatus("Convirtiendo a 3MF...");
                        bool success = await ConvertTo3MF(file, convertedFilePath);
                        if (!success)
                        {
                            failedRepairs.Add(convertedFilePath);
                            continue;
                        }
                    }
                    else
                    {
                        UpdateStatus("El archivo ya es 3mf, procediendo a reparación...");
                        convertedFilePath = file;
                    }
                    
                    // Repair
                    if (File.Exists(convertedFilePath))
                    {
                        bool success = await TryRepairFile(convertedFilePath);
                        if (!success)
                        {
                            failedRepairs.Add(convertedFilePath);
                            continue;
                        }

                        // Convert to STL if requested
                        if (outputFormat.Equals("stl", StringComparison.OrdinalIgnoreCase))
                        {
                            string stlOutputPath = Path.ChangeExtension(convertedFilePath, ".stl");
                            bool stlSuccess = await ConvertToSTL(convertedFilePath, stlOutputPath);
                            if (stlSuccess)
                            {
                                // Delete the intermediate 3MF file
                                File.Delete(convertedFilePath);
                                UpdateStatus("Listo (STL).", true);
                            }
                            else
                            {
                                UpdateStatus("Fallo conversión STL, se mantiene 3MF.", true);
                            }
                        }
                    }
                    else
                    {
                        UpdateStatus($"No se encuentra el archivo {convertedFilePath}", true);
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
                            // Silent unless error/debugging needed
                            // PrintColored($"\t{e.Data}", ConsoleColor.Yellow);
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
                        // PrintColored("\tConversion successful.", ConsoleColor.Yellow);
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

        #region STL Conversion
        static async Task<bool> ConvertToSTL(string inputFilePath, string outputFilePath)
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
                "--export-stl",
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
                             // Silent
                            // PrintColored($"\t{e.Data}", ConsoleColor.Yellow);
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
                        // PrintColored($"\tConverted to STL: {Path.GetFileName(outputFilePath)}", ConsoleColor.Green);
                        return true;
                    }
                    else
                    {
                        PrintColored($"\tSTL conversion process exited with code {process.ExitCode}", ConsoleColor.Red);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintColored($"\tAn error occurred during STL conversion: {ex.Message}", ConsoleColor.Red);
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
                UpdateStatus("Verificando malla...");
                bool preRepairVerifySuccess = await VerifyMeshes(model);
                if (preRepairVerifySuccess)
                {
                    if (outputFormat != "stl") 
                    {
                       UpdateStatus("Sin errores, finalizado.", true);
                    }
                    return true;
                }
                else
                {
                    UpdateStatus("Errores encontrados: reparando...");
                }

                // Attempt repair with timeout
                bool repairSuccess = await RepairWithTimeoutAsync(model, TimeSpan.FromSeconds(timeoutSeconds));

                // Verify the fix
                UpdateStatus("Verificando reparación...");
                bool postRepairVerifySuccess = await VerifyMeshes(model);
                if (postRepairVerifySuccess)
                {
                    if (outputFormat != "stl")
                    {
                        UpdateStatus("Reparado correctamente.", true);
                    }
                }
                else
                {
                    UpdateStatus("Fallo al verificar reparación.", true);
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
                        return true;
                    }
                    else
                    {
                        UpdateStatus("Fallo en la tarea de reparación.");
                        return false;
                    }
                }
                else
                {
                    cts.Cancel();

                    UpdateStatus("Tiempo de espera agotado.", true);
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
        static Dictionary<string, string> InteractiveMode()
        {
            var arguments = new Dictionary<string, string>();

            try
            {
                // Input File/Folder
                Console.Write("\nInput Folder or File (press Enter for current directory, or add '-d' for defaults): ");
                string input = Console.ReadLine()?.Trim().Trim('\"');
                
                // Check if user entered -d flag
                if (!string.IsNullOrEmpty(input) && input.Equals("-d", StringComparison.OrdinalIgnoreCase))
                {
                    useDefaults = true;
                    input = string.Empty;
                }
                else if (!string.IsNullOrEmpty(input) && input.EndsWith(" -d", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract filename and set defaults
                    useDefaults = true;
                    input = input.Substring(0, input.Length - 3).Trim();
                }
                
                // If empty, use current directory
                if (string.IsNullOrEmpty(input))
                {
                    input = Directory.GetCurrentDirectory();
                    PrintColored($"Using current directory: {input}", ConsoleColor.Cyan);
                }
                
                arguments[InputFilePathArg] = input;

                // If useDefaults is true, skip all other prompts and use defaults
                if (useDefaults)
                {
                    PrintColored("Using default settings for all options.", ConsoleColor.Cyan);
                    arguments[OutputFormatArg] = "3mf";
                    arguments[CloneFolderHierarchyArg] = "true";
                    // outputFilePath remains empty (same location)
                    // timeoutSeconds uses default
                }
                else
                {
                    // Output File/Folder (optional)
                    Console.Write("Output Folder (press Enter to use same location): ");
                    string output = Console.ReadLine()?.Trim().Trim('\"');
                    if (!string.IsNullOrEmpty(output))
                    {
                        arguments[OutputFilePathArg] = output;
                    }

                    // Output Format
                    Console.Write("Convert to STL after repair? (Y/N, default: N): ");
                    string stlChoice = Console.ReadLine()?.Trim().ToUpper();
                    if (stlChoice == "Y" || stlChoice == "YES")
                    {
                        arguments[OutputFormatArg] = "stl";
                    }
                    else
                    {
                        arguments[OutputFormatArg] = "3mf";
                    }

                    // Additional options
                    Console.Write("Clone folder hierarchy? (Y/N, default: Y): ");
                    string cloneChoice = Console.ReadLine()?.Trim().ToUpper();
                    if (cloneChoice == "N" || cloneChoice == "NO")
                    {
                        arguments[CloneFolderHierarchyArg] = "false";
                    }
                    else
                    {
                        arguments[CloneFolderHierarchyArg] = "true";
                    }

                    Console.Write("Timeout per file in seconds (default: 600): ");
                    string timeout = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out int timeoutValue))
                    {
                        arguments[TimeoutSecondsArg] = timeout;
                    }
                }

                Console.WriteLine();
                return arguments;
            }
            catch (Exception ex)
            {
                PrintColored($"Error in interactive mode: {ex.Message}", ConsoleColor.Red);
                return null;
            }
        }

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
            outputFormat = GetArgumentValue(arguments, OutputFormatArg, "3mf").ToLower();

            // Validate output format
            if (outputFormat != "3mf" && outputFormat != "stl")
            {
                PrintColored($"Invalid output format '{outputFormat}'. Supported formats: 3mf, stl", ConsoleColor.Red);
                return false;
            }

            return true;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"--{InputFilePathArg}=<input_file_path>     Specify the path to an individual model or a folder of models to repair.");
            Console.WriteLine($"--{CloneFolderHierarchyArg}=<true>         Optional: If a folder of models is specified, clone the folder hierarchy with the repaired files. Default is true.");
            Console.WriteLine($"--{OutputFilePathArg}=<output_file_path>   Optional: Specify a folder to put the repaired files.");
            Console.WriteLine($"--{OutputFormatArg}=<3mf|stl>              Optional: Specify output format (3mf or stl). Default is 3mf.");
            Console.WriteLine($"--{TimeoutSecondsArg}=<60>                 Optional: Specify how long to repair a model before giving up. Default is 60 seconds.");
            Console.WriteLine("-d                                          Use default settings for all options (can be combined with other flags).");
            Console.WriteLine("--help                                      Show help information.");
            Console.WriteLine("\nInteractive Mode:");
            Console.WriteLine("  Run without arguments to enter interactive mode.");
            Console.WriteLine("  Press Enter at first prompt to use current directory.");
            Console.WriteLine("  Add '-d' to use default settings (e.g., 'model.stl -d' or just '-d').");
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

        static void UpdateStatus(string status, bool finalize = false)
        {
            string prefix = $"[{currentFileIndex}/{totalFilesCount}] {currentFileName}: ";
            string fullMessage = prefix + status;
            
            try 
            {
                if (!Console.IsOutputRedirected)
                {
                    int width = Console.WindowWidth;
                    // Pad with spaces to ensure we clear previous shorter lines
                    if (fullMessage.Length >= width)
                    {
                        fullMessage = fullMessage.Substring(0, width - 4) + "...";
                    }
                    
                    Console.Write("\r" + fullMessage.PadRight(width - 1));
                    
                    if (finalize)
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    // If redirected, only print finalized lines to avoid log spam
                    if (finalize)
                    {
                         Console.WriteLine(fullMessage);
                    }
                }
            }
            catch
            {
                // Fallback
                 if (finalize) Console.WriteLine(fullMessage);
            }
        }

        static void PrintHeader()
        {
            PrintColored(@"-------------------------------------------------------------------
MeshRepair V3.1.0 - https://github.com/SynrgStudio/MeshRepair", ConsoleColor.Blue);
            PrintColored(@"MeshRepair v1.0.0 - https://github.com/arnasdev/Windows3MFRepairCLI", ConsoleColor.White);
            PrintColored(@"-------------------------------------------------------------------", ConsoleColor.Blue);
        }
        #endregion
    }
}