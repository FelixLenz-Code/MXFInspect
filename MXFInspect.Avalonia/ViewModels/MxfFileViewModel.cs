using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myriadbits.MXF;
using MXFInspect.Avalonia.Services;

namespace MXFInspect.Avalonia.ViewModels;

/// <summary>
/// Represents one open MXF file (one tab). Owns the parsed <see cref="MXFFile"/>,
/// the physical and logical trees, the property grid, the hex view and the
/// conformance report.
/// </summary>
public partial class MxfFileViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    public string FilePath { get; }
    public string Header { get; }

    public ObservableCollection<MxfNodeViewModel> PhysicalRoot { get; } = new();
    public ObservableCollection<MxfNodeViewModel> LogicalRoot { get; } = new();
    public ObservableCollection<PropertyRow> Properties { get; } = new();
    public ObservableCollection<ValidationRow> Validations { get; } = new();

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _status = "Loading…";
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _excludeFiller;
    [ObservableProperty] private string _typeFilter = string.Empty;

    [ObservableProperty] private MxfNodeViewModel? _selectedNode;
    [ObservableProperty] private byte[] _hexBytes = Array.Empty<byte>();
    [ObservableProperty] private long _hexBaseOffset;

    public MXFFile? File { get; private set; }

    public MxfFileViewModel(string filePath, SettingsService settings)
    {
        _settings = settings;
        FilePath = filePath;
        Header = Path.GetFileName(filePath);
        ExcludeFiller = settings.ExcludeFiller;
    }

    public async Task LoadAsync()
    {
        try
        {
            var file = await Task.Run(() =>
            {
                var worker = new BackgroundWorker { WorkerReportsProgress = true };
                worker.ProgressChanged += (_, e) =>
                    Dispatcher.UIThread.Post(() =>
                    {
                        Progress = e.ProgressPercentage;
                        if (e.UserState is string s) Status = s;
                    });
                return new MXFFile(FilePath, worker);
            });

            File = file;
            RebuildPhysicalTree();
            BuildLogicalTree();
            Status = $"{file.PartitionCount} partitions, {file.Filesize:N0} bytes";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Status = "Failed to parse file";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RebuildPhysicalTree()
    {
        if (File == null) return;
        PhysicalRoot.Clear();
        var filter = new TreeFilter(ExcludeFiller, TypeFilter);
        foreach (var child in File.Children.Where(filter.Matches).OrderBy(o => o.Offset))
            PhysicalRoot.Add(MxfNodeViewModel.ForPhysical(child, _settings, filter));
    }

    private void BuildLogicalTree()
    {
        if (File?.LogicalBase == null) return;
        LogicalRoot.Clear();
        LogicalRoot.Add(MxfNodeViewModel.ForLogical(File.LogicalBase, _settings, TreeFilter.None));
    }

    partial void OnExcludeFillerChanged(bool value)
    {
        _settings.ExcludeFiller = value;
        RebuildPhysicalTree();
    }

    partial void OnTypeFilterChanged(string value) => RebuildPhysicalTree();

    partial void OnSelectedNodeChanged(MxfNodeViewModel? value)
    {
        Properties.Clear();
        if (value?.Object == null) return;

        foreach (var row in PropertyRow.FromObject(value.Object))
            Properties.Add(row);

        LoadHex(value.Object);
    }

    private void LoadHex(MXFObject obj)
    {
        const int maxBytes = 64 * 1024; // cap so multi-GB essence never floods the view
        try
        {
            using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(obj.Offset, SeekOrigin.Begin);
            long len = Math.Max(0, Math.Min(obj.Length, maxBytes));
            var buffer = new byte[len];
            int read = fs.Read(buffer, 0, buffer.Length);
            HexBaseOffset = obj.Offset;
            HexBytes = read == buffer.Length ? buffer : buffer[..read];
        }
        catch
        {
            HexBytes = Array.Empty<byte>();
        }
    }

    [RelayCommand]
    private void NextOfType()
    {
        if (SelectedNode?.Object is not { } current) return;
        var next = current.FindNextObjectOfType(current.GetType());
        SelectInPhysicalTree(next);
    }

    [RelayCommand]
    private void PreviousOfType()
    {
        if (SelectedNode?.Object is not { } current) return;
        var prev = current.FindPreviousObjectOfType(current.GetType());
        SelectInPhysicalTree(prev);
    }

    /// <summary>Expands the physical tree down to <paramref name="target"/> and selects it.</summary>
    private void SelectInPhysicalTree(MXFObject? target)
    {
        if (target == null) return;

        // Build the ancestor chain (root first).
        var chain = new List<MXFObject>();
        for (var o = target; o != null; o = o.Parent)
            chain.Add(o);
        chain.Reverse();

        IEnumerable<MxfNodeViewModel> level = PhysicalRoot;
        MxfNodeViewModel? node = null;
        foreach (var obj in chain)
        {
            node = level.FirstOrDefault(n => ReferenceEquals(n.Object, obj));
            if (node == null) return; // filtered out
            node.EnsureChildrenLoaded();
            node.IsExpanded = true;
            level = node.Children;
        }

        if (node != null)
        {
            node.IsSelected = true;
            SelectedNode = node;
        }
    }

    [RelayCommand]
    private Task RunTests() => RunValidationAsync();

    public async Task RunValidationAsync()
    {
        if (File == null) return;
        Status = "Running conformance tests…";
        await Task.Run(() =>
        {
            var worker = new BackgroundWorker { WorkerReportsProgress = true };
            File.ExecuteValidationTest(worker, true);
        });

        Validations.Clear();
        foreach (var result in File.Results)
        {
            foreach (var detail in result)
            {
                Validations.Add(new ValidationRow
                {
                    Category = result.Category,
                    State = detail.State.ToString(),
                    Result = detail.Result,
                    Brush = ValidationRow.BrushFor(detail.State),
                });
            }
        }
        Status = $"Conformance: {Validations.Count} results";
    }
}
