using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ModernWpf.Controls;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MeshRepairGUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private bool _isProcessing = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    #region Browse Events
    private void BrowseInputFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select 3D Model File",
            Filter = "All Supported Files|*.stl;*.step;*.stp;*.3mf;*.obj;*.amf|" +
                     "STL Files|*.stl|" +
                     "3MF Files|*.3mf|" +
                     "OBJ Files|*.obj|" +
                     "STEP Files|*.step;*.stp|" +
                     "AMF Files|*.amf|" +
                     "All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InputPathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseInputFolder_Click(object sender, RoutedEventArgs e)
    {
        using (var dialog = new FolderBrowserDialog())
        {
            dialog.Description = "Select Folder Containing 3D Models";
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InputPathTextBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        using (var dialog = new FolderBrowserDialog())
        {
            dialog.Description = "Select Output Folder";
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputPathTextBox.Text = dialog.SelectedPath;
            }
        }
    }
    #endregion

    #region Drag & Drop
    private void OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                InputPathTextBox.Text = files[0];
            }
        }
    }
    #endregion

    #region Actions
    private async void StartRepair_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            await ShowMessageAsync("Already Processing", "Please wait for the current operation to complete.");
            return;
        }

        if (string.IsNullOrWhiteSpace(InputPathTextBox.Text))
        {
            await ShowMessageAsync("Input Required", "Please select an input file or folder.");
            return;
        }

        if (!File.Exists(InputPathTextBox.Text) && !Directory.Exists(InputPathTextBox.Text))
        {
            await ShowMessageAsync("Invalid Path", "The specified input path does not exist.");
            return;
        }

        _isProcessing = true;
        StartButton.IsEnabled = false;
        MainProgressBar.Visibility = Visibility.Visible;
        MainProgressBar.IsIndeterminate = true;

        try
        {
            await RunRepairProcess();
        }
        catch (Exception ex)
        {
            AppendOutput($"\n❌ Error: {ex.Message}\n", true);
            await ShowMessageAsync("Error", $"An error occurred during repair:\n\n{ex.Message}");
        }
        finally
        {
            _isProcessing = false;
            StartButton.IsEnabled = true;
            MainProgressBar.Visibility = Visibility.Collapsed;
            MainProgressBar.IsIndeterminate = false;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        OutputTextBlock.Text = "Ready to repair models. Select input path and click Start.";
    }

    private async Task RunRepairProcess()
    {
        AppendOutput("=== MeshRepair Process Started ===\n", false);
        AppendOutput($"Input: {InputPathTextBox.Text}\n", false);
        AppendOutput($"Output: {(string.IsNullOrWhiteSpace(OutputPathTextBox.Text) ? "Same as input" : OutputPathTextBox.Text)}\n", false);
        AppendOutput($"Format: {(ConvertToStlCheckBox.IsChecked == true ? "STL" : "3MF")}\n", false);
        AppendOutput($"Clone Hierarchy: {(CloneFolderHierarchyCheckBox.IsChecked == true ? "Yes" : "No")}\n", false);
        AppendOutput($"Timeout: {(int)TimeoutNumberBox.Value}s\n\n", false);

        // Build command line arguments
        var args = new System.Collections.Generic.List<string>
        {
            $"--inputFilePath={InputPathTextBox.Text}"
        };

        if (!string.IsNullOrWhiteSpace(OutputPathTextBox.Text))
        {
            args.Add($"--outputFilePath={OutputPathTextBox.Text}");
        }

        if (ConvertToStlCheckBox.IsChecked == true)
        {
            args.Add($"--outputFormat=stl");
        }

        args.Add($"--cloneFolderHierarchy={CloneFolderHierarchyCheckBox.IsChecked}");
        args.Add($"--timeoutSeconds={(int)TimeoutNumberBox.Value}");

        AppendOutput("Starting repair...\n\n", false);

        // Call the CLI Main method in a background task
        await Task.Run(async () =>
        {
            try
            {
                // Redirect console output to our UI
                var originalOut = Console.Out;
                using (var writer = new StringWriter())
                {
                    Console.SetOut(writer);

                    // Run the CLI program
                    await MeshRepairCLI.Program.Main(args.ToArray());

                    // Get the output
                    var output = writer.ToString();
                    Dispatcher.Invoke(() =>
                    {
                        AppendOutput(output, false);
                    });

                    Console.SetOut(originalOut);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    AppendOutput($"\n❌ Error during repair: {ex.Message}\n", true);
                });
            }
        });

        AppendOutput("\n=== Process Completed ===\n", false);
    }

    private void AppendOutput(string text, bool isError = false)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendOutput(text, isError));
            return;
        }

        OutputTextBlock.Text += text;
        OutputScrollViewer.ScrollToBottom();
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };

        await dialog.ShowAsync();
    }
    #endregion
}