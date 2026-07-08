using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MXFInspect.Avalonia.ViewModels;

namespace MXFInspect.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open MXF file(s)",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("MXF files") { Patterns = new[] { "*.mxf", "*.MXF" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*" } },
            },
        });

        if (files.Count == 0 || Vm == null)
            return;

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrEmpty(p))
            .Cast<string>()
            .ToList();

        await Vm.OpenFilesAsync(paths);
    }

    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private void OnToggleHexOffset(object? sender, RoutedEventArgs e)
    {
        // IsChecked is already bound two-way; nothing else needed. Kept for clarity/extension.
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        await new AboutWindow().ShowDialog(this);
    }

    private async void OnExportReportClick(object? sender, RoutedEventArgs e)
    {
        var file = Vm?.SelectedFile;
        if (file == null)
            return;

        if (file.Validations.Count == 0)
            await file.RunValidationAsync();

        var target = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export conformance report",
            SuggestedFileName = Path.GetFileNameWithoutExtension(file.Header) + "-report.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new[] { new FilePickerFileType("Text report") { Patterns = new[] { "*.txt" } } },
        });

        if (target == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"MXFInspect conformance report");
        sb.AppendLine($"File: {file.FilePath}");
        sb.AppendLine(new string('-', 60));
        foreach (var row in file.Validations)
            sb.AppendLine($"[{row.State,-8}] {row.Category}: {row.Result}");

        await using var stream = await target.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(sb.ToString());
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TreeView { SelectedItem: MxfNodeViewModel node } tree &&
            tree.DataContext is MxfFileViewModel fileVm)
        {
            fileVm.SelectedNode = node;
        }
    }
}
