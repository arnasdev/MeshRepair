using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Printing3D;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MeshRepairCLI
{
    class Program
    {
        static bool overwrite = true;
        static string inputFilePath = "";
        static string outputFilePath = "";

        static async Task Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Full paths only");
                Console.WriteLine("Usage: MeshRepairCLI <repair_file.3mf>");
                Console.WriteLine("or");
                Console.WriteLine("Usage: MeshRepairCLI <input_file.3mf> <output_file.3mf>");
            }

            if (args.Length == 1)
            {
                overwrite = true;
                inputFilePath = args[0];
                outputFilePath = System.IO.Path.Join(System.IO.Path.GetDirectoryName(args[0]), "temp");
            }
            else if (args.Length == 2)
            {
                overwrite = false;
                inputFilePath = args[0];
                outputFilePath = args[1];
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

                // Repair the model
                await model.RepairAsync();

                // Save the repaired model back into the 3MF package
                await package.SaveModelToPackageAsync(model);

                if (overwrite)
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
    }
}