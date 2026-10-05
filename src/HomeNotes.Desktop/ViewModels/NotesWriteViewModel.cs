using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNotes.Desktop.Interface;
using HomeNotes.Desktop.Services;

namespace HomeNotes.Desktop.ViewModels
{
    public partial class NotesWriteViewModel : ViewModelBase
    {
        private readonly IStorage _storage;
        private readonly IDialogService _dialogService;
        private readonly string[] _filterfile = [".md", ".png", ".img", ".webp"];


        private FileSystemItem? _fileSystemItem;
        [ObservableProperty] private ObservableCollection<FileSystemItem> _fileSystemItems = new();

        public NotesWriteViewModel(IStorage storage, IDialogService dialogService, Guid? UserId = null)
        {
            _storage = storage;
            _dialogService = dialogService;
            loadTree();
        }

        [RelayCommand]
        public async Task ChangeFolderAsync()
        {
            var path = await _dialogService.SelectFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                _storage.StoragePath = path;
                _storage.Save();
                loadTree();
            }
        }

        public void loadTree()
        {
            FileSystemItems.Clear();
            var rootPath = _storage.StoragePath;
    
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return;
            }

      
            var folderName = new DirectoryInfo(rootPath).Name;
    

            if (string.IsNullOrWhiteSpace(folderName))
            {
                folderName = rootPath; 
            }

            _fileSystemItem = new FileSystemItem(folderName, rootPath, true);
            PopulateNode(_fileSystemItem);
    
            FileSystemItems.Add(_fileSystemItem);
        }

        public void PopulateNode(FileSystemItem node)
        {
            if (!node.IsDirectory)
            {
                return;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(node.Path))
                {
                    var dirNode = new FileSystemItem(System.IO.Path.GetFileName(dir), dir, true);
                    PopulateNode(dirNode);
                    node.SubItems.Add(dirNode);
                }

                var allFiles = Directory.GetFiles(node.Path);
                foreach (var file in allFiles)
                {
                    var extension = Path.GetExtension(file).ToLower();
                    if (_filterfile.Contains(extension))
                    {
                        node.SubItems.Add(new FileSystemItem(Path.GetFileName(file), file, false));
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}