using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNotes.Desktop.Interface;
using HomeNotes.Desktop.Markdown.Preview;
using HomeNotes.Desktop.Services; 

namespace HomeNotes.Desktop.ViewModels
{
    public partial class NotesWriteViewModel : ViewModelBase
    {
        public string? VaultRootPath => _storage.StoragePath;
        private readonly IStorage _storage;
        private readonly IDialogService _dialogService;
        private readonly string[] _filterfile = [".md", ".png", ".img", ".webp"];
        
        public MarkdownPreviewManager PreviewManager { get; }

        [ObservableProperty] private ObservableCollection<FileSystemItem> _fileSystemItems = new();
        [ObservableProperty] private FileSystemItem? _selectedItem;
        [ObservableProperty] private Bitmap? _selectedImage;
        [ObservableProperty] private bool _isEditorVisible = false;
        [ObservableProperty] private bool _isImageVisible = false;
        [ObservableProperty] private string? _titleFileSystem;
        [ObservableProperty] private TextDocument _document = new();

        private FileSystemItem? _rootFolderItem;
        private CancellationTokenSource? _saveCts;
        private bool _isLoadingFile = false;
        
     
        private bool _isRenaming = false; 

        public NotesWriteViewModel(
            MarkdownPreviewManager previewManager,
            IStorage storage,
            IDialogService dialogService,
            Guid? UserId = null)
        {
            PreviewManager = previewManager;
            _storage = storage;
            _dialogService = dialogService;

            Document.TextChanged += Document_TextChanged;

            LoadTree();
        }

        [RelayCommand]
        public async Task ChangeFolderAsync()
        {
            var path = await _dialogService.SelectFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                _storage.StoragePath = path;
                _storage.Save();
                LoadTree();
            }
        }

        public void LoadTree()
        {
            TitleFileSystem = "";
            FileSystemItems.Clear();

            var rootPath = _storage.StoragePath;
            PreviewManager.VaultRootPath = rootPath;
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath)) return;

            var folderName = new DirectoryInfo(rootPath).Name;
            if (string.IsNullOrWhiteSpace(folderName)) folderName = rootPath;

            _rootFolderItem = new FileSystemItem(folderName, rootPath, true);
            PopulateNode(_rootFolderItem);
            TitleFileSystem = _rootFolderItem.Name;
            FileSystemItems.Add(_rootFolderItem);
        }

        public void PopulateNode(FileSystemItem node)
        {
            if (!node.IsDirectory) return;

            try
            {
                foreach (var dir in Directory.GetDirectories(node.Path))
                {
                    var dirNode = new FileSystemItem(Path.GetFileName(dir), dir, true);
                    PopulateNode(dirNode);
                    node.SubItems?.Add(dirNode);
                }

                var allFiles = Directory.GetFiles(node.Path);
                foreach (var file in allFiles)
                {
                    var extension = Path.GetExtension(file).ToLower();
                    if (_filterfile.Contains(extension))
                    {
                        node.SubItems?.Add(new FileSystemItem(Path.GetFileName(file), file, false));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Игнорируем закрытые папки
            }
        }

        [RelayCommand]
        public void CreateNote()
        {
            string path = _storage.StoragePath;
            if (string.IsNullOrEmpty(path)) return;

            string baseName = "untitled";
            string title = $"{baseName}.md";
            int count = 0;
            string filename = Path.Combine(path, title);
            
            while (File.Exists(filename))
            {
                count++;
                baseName = $"untitled{count}";
                title = $"{baseName}.md";
                filename = Path.Combine(path, title);
            }
            
            string initialContent = $"# {baseName}\n\n";

            try
            {
                _saveCts?.Cancel();
                _isLoadingFile = true; 

                File.WriteAllText(filename, initialContent);

                var node = new FileSystemItem(title, filename, false);
                _rootFolderItem?.SubItems?.Add(node);

                Document.TextChanged -= Document_TextChanged;
                Document.Text = initialContent;
                Document.TextChanged += Document_TextChanged;

                SelectedItem = node;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании файла: {ex.Message}");
            }
            finally
            {
                _isLoadingFile = false; 
            }
        }

        partial void OnSelectedItemChanged(FileSystemItem? value)
        {
        
            if (_isRenaming) return; 

            if (value == null || value.IsDirectory)
            {
                IsEditorVisible = false;
                IsImageVisible = false;
                return;
            }

            var extension = Path.GetExtension(value.Path).ToLower();

            if (extension == ".md")
            {
                IsImageVisible = false;
                IsEditorVisible = true;

                _isLoadingFile = true;
                try
                {
                    var text = File.ReadAllText(value.Path);

                    Document.TextChanged -= Document_TextChanged;
                    Document.Text = text;
                    Document.TextChanged += Document_TextChanged;
                }
                catch (Exception ex)
                {
                    Document.Text = $"Ошибка загрузки: {ex.Message}";
                }
                finally
                {
                    _isLoadingFile = false;
                }
            }
            else if (_filterfile.Contains(extension))
            {
                IsEditorVisible = false;
                IsImageVisible = true;
                try
                {
                    SelectedImage?.Dispose();
                    SelectedImage = new Bitmap(value.Path);
                }
                catch
                {
                }
            }
        }

        private void Document_TextChanged(object? sender, EventArgs e)
        {
            if (_isLoadingFile || SelectedItem == null || !IsEditorVisible) return;

            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            _ = SaveDebouncedAsync(Document.Text, SelectedItem, _saveCts.Token);
        }

        private async Task SaveDebouncedAsync(string text, FileSystemItem item, CancellationToken token)
        {
            try
            {
                await Task.Delay(700, token);
                if (token.IsCancellationRequested) return;

                bool fileRenamed = TryRenameFileBasedOnHeader(text, item);
                if (token.IsCancellationRequested) return;

            
                var pathToSave = SelectedItem?.Path ?? item.Path;
                await File.WriteAllTextAsync(pathToSave, text, token);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool TryRenameFileBasedOnHeader(string text, FileSystemItem item)
        {
            if (string.IsNullOrWhiteSpace(text) || item == null) return false;

            string firstLine = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            if (!firstLine.StartsWith("# ")) return false;

            string newName = firstLine.Substring(2).Trim();
            if (string.IsNullOrWhiteSpace(newName)) return false;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            newName = string.Join("_", newName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            string newFileName = $"{newName}.md";
            if (item.Name.Equals(newFileName, StringComparison.OrdinalIgnoreCase)) return false;

            string directory = Path.GetDirectoryName(item.Path) ?? _storage.StoragePath;
            if (string.IsNullOrEmpty(directory)) return false;

            string newFullPath = Path.Combine(directory, newFileName);
            if (File.Exists(newFullPath)) return false; 

            try
            {
                File.Move(item.Path, newFullPath);

           
                var newItem = item with { Name = newFileName, Path = newFullPath };

       
                var parentCollection = FindParentCollection(FileSystemItems, item);
                if (parentCollection != null)
                {
                    int index = parentCollection.IndexOf(item);
                    if (index >= 0)
                    {
                        _isRenaming = true; 

                    
                        parentCollection[index] = newItem; 
                        SelectedItem = newItem;

                        _isRenaming = false; 
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка переименования: {ex.Message}");
                return false;
            }
        }

        // Рекурсивный поиск коллекции, в которой лежит конкретный файл
        private ObservableCollection<FileSystemItem>? FindParentCollection(ObservableCollection<FileSystemItem> items, FileSystemItem target)
        {
            if (items.Contains(target)) return items;

            foreach (var item in items)
            {
                if (item.IsDirectory && item.SubItems != null)
                {
                    var found = FindParentCollection(item.SubItems, target);
                    if (found != null) return found;
                }
            }
            return null;
        }
    }
}