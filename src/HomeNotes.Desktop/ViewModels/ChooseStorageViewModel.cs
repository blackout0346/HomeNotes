using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using HomeNotes.Desktop.Interface;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace HomeNotes.Desktop.ViewModels;

public partial class ChooseStorageViewModel : ViewModelBase
{
    public event Action? StorageReady;
    public event Action? BackRequested;
    public IStorage _storage;
    public MainWindowViewModel _mainWindowViewModel;


    public ChooseStorageViewModel(IStorage storage, MainWindowViewModel mainWindowViewModel)
    {

        _mainWindowViewModel = mainWindowViewModel;
        _storage = storage;
    }

    [RelayCommand]
    public async Task SelectStorageFolder()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.StorageProvider is { } storageProvider)
        {
            var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Выберите папку для заметок",
                AllowMultiple =  false
            });
          
            if (folder.Count > 0)
            {
                SaveAndProceed(folder[0].Path.LocalPath);
            }
        }
    }

    private void SaveAndProceed(string path)
    {
        _storage.StoragePath = path;
        _storage.Save();
        StorageReady?.Invoke();
    }
    [RelayCommand]
    void QuickStart()
    {
        var defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HomeNotesVault");
        if (!Directory.Exists(defaultFolder))
        {
            Directory.CreateDirectory(defaultFolder);
        }
        SaveAndProceed(defaultFolder);
    }

    [RelayCommand]
    void Back()
    {
        BackRequested?.Invoke();
    }
}