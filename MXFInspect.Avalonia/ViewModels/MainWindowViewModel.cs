using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MXFInspect.Avalonia.Services;

namespace MXFInspect.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public SettingsService Settings { get; }

    public ObservableCollection<MxfFileViewModel> Files { get; } = new();

    [ObservableProperty] private MxfFileViewModel? _selectedFile;

    [ObservableProperty] private bool _showOffsetAsHex;

    public string Title => "MXFInspect — cross-platform MXF structure viewer";

    public MainWindowViewModel()
    {
        Settings = SettingsService.Load();
        ShowOffsetAsHex = Settings.ShowOffsetAsHex;
    }

    public async Task OpenFilesAsync(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var vm = new MxfFileViewModel(path, Settings);
            Files.Add(vm);
            SelectedFile = vm;
            await vm.LoadAsync();
        }
    }

    [RelayCommand]
    private void CloseFile(MxfFileViewModel? file)
    {
        if (file == null) return;
        Files.Remove(file);
        if (ReferenceEquals(SelectedFile, file))
            SelectedFile = Files.Count > 0 ? Files[^1] : null;
    }

    [RelayCommand]
    private async Task RunValidation()
    {
        if (SelectedFile != null)
            await SelectedFile.RunValidationAsync();
    }

    partial void OnShowOffsetAsHexChanged(bool value)
    {
        Settings.ShowOffsetAsHex = value;
        Settings.Save();
    }
}
