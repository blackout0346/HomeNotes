using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using HomeNotes.Desktop.Interface;

namespace HomeNotes.Desktop.Services
{
    public class DialogService : IDialogService
    {
        public async Task<string> SelectFolderAsync()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.StorageProvider is { } provider)
            {
                var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Выберите папку для заметок",
                    AllowMultiple =  false
                });
                if (folders.Count > 0)
                {
                    return folders[0].Path.LocalPath;
                }
            }

            return null;
        }
    }
}