using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Printing3D;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MeshRepairCLI
{
    class Program
    {
        static string inputFilePath = "";
        static string outputFilePath = "";
        static int timeoutSeconds = 60 * 10;
        static bool overwriteOriginal = true;

        static readonly string inputFilePathArgument = "inputFilePath";
        static readonly string outputFilePathArgument = "outputFilePath";
        static readonly string timeoutSecondsArgument = "timeoutSeconds";
        static readonly string helpArgument = "help";

        static async Task Main(string[] args)
        {
            var arguments = ParseArguments(args);
            if (!ValidateArguments(arguments))
            {
                return;
            }

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
                    Console.WriteLine("Mesh verified with no errors, skipping repair");
                    return;
                }
                else
                {
                    Console.WriteLine("Found errors - proceeding with repair");
                }

                // Attempt repair with timeout
                bool repairSuccess = await RepairWithTimeoutAsync(model, TimeSpan.FromSeconds(timeoutSeconds));

                // Verify the fix
                bool postRepairVerifySuccess = await VerifyMeshes(model);
                if (postRepairVerifySuccess)
                {
                    Console.WriteLine("Verification successful");
                }
                else
                {
                    Console.WriteLine("Failed to verify mesh, exiting without saving");
                    return;
                }

                // Save the repaired model back into the 3MF package
                await package.SaveModelToPackageAsync(model);

                if (overwriteOriginal)
                {
                    await Overwrite3MF(package, inputFile);
                }
                else
                {
                    await SaveSeperate3MF(package);
                }
             
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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
                        Console.WriteLine("Finished repair.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed repair.");
                        return false;
                    }
                }
                else
                {
                    cts.Cancel();

                    Console.WriteLine("Repair exceeded timeout, cancelled");
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

        #region Saving
        static async Task Overwrite3MF(Printing3D3MFPackage package, StorageFile originalFile)
        {
            // Save the 3MF seperately anyway
            await SaveSeperate3MF(package);

            StorageFile outputFile = await StorageFile.GetFileFromPathAsync(outputFilePath);

            // Delete the original file
            await originalFile.DeleteAsync();

            // Rename the new file to the original
            await outputFile.RenameAsync(System.IO.Path.GetFileName(inputFilePath));
        }

        static async Task SaveSeperate3MF(Printing3D3MFPackage package)
        {
            IRandomAccessStream saveStream = await package.SaveAsync();
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(outputFilePath));
            StorageFile outputFile = await storageFolder.CreateFileAsync(System.IO.Path.GetFileName(outputFilePath), CreationCollisionOption.ReplaceExisting);
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

            if (arguments.ContainsKey(helpArgument))
            {
                ShowHelp();
                return false;
            }

            if (!arguments.ContainsKey(inputFilePathArgument))
            {
                ShowHelp();
                return false;
            }

            if (arguments.ContainsKey(inputFilePathArgument) && !arguments.ContainsKey(outputFilePathArgument))
            {
                overwriteOriginal = true;
                inputFilePath = GetArgumentValue(arguments, inputFilePathArgument);
                outputFilePath = System.IO.Path.Join(System.IO.Path.GetDirectoryName(inputFilePath), "temp");
            }
            else if (arguments.ContainsKey(inputFilePathArgument) && arguments.ContainsKey(outputFilePathArgument))
            {
                overwriteOriginal = false;
                inputFilePath = GetArgumentValue(arguments, inputFilePathArgument);
                outputFilePath = GetArgumentValue(arguments, outputFilePathArgument);
            }

            timeoutSeconds = GetArgumentValue(arguments, timeoutSecondsArgument, timeoutSeconds);

            return true;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"--{inputFilePathArgument}=<input_file_path>     Specify the input 3MF file path.");
            Console.WriteLine($"--{outputFilePathArgument}=<output_file_path>   Optional: Specify the output 3MF file path, otherwise will overwrite the input path.");
            Console.WriteLine($"--{timeoutSecondsArgument}=<60>                 Optional: Specify whether to repair the model. Default is 600 seconds.");
            Console.WriteLine("--help                                           Show help information.");
        }

        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                var splitArg = arg.Split('=');
                if (splitArg.Length == 2)
                {
                    arguments[splitArg[0].TrimStart('-')] = splitArg[1];
                }
                else
                {
                    arguments[arg.TrimStart('-')] = null;
                }
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
    }
}