using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using AvaloniaEdit.Document; // Обязательно для TextDocument
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

        private readonly MarkdownLiveColorizer _colorizer;
        public MarkdownLiveColorizer Colorizer => _colorizer;

        [ObservableProperty] private ObservableCollection<FileSystemItem> _fileSystemItems = new();

        private FileSystemItem? _rootFolderItem;

        [ObservableProperty] private FileSystemItem? _selectedItem;
        [ObservableProperty] private Bitmap? _selectedImage;
        [ObservableProperty] private bool _isEditorVisible = false;
        [ObservableProperty] private bool _isImageVisible = false;

        // НАТИВНЫЙ ДОКУМЕНТ AVALONIAEDIT
        [ObservableProperty] private TextDocument _document = new();

        private CancellationTokenSource? _saveCts;
        private bool _isLoadingFile = false;

        public NotesWriteViewModel(MarkdownLiveColorizer colorizer, IStorage storage, IDialogService dialogService,
            Guid? UserId = null)
        {
            _colorizer = colorizer;
            _storage = storage;
            _dialogService = dialogService;

            // Напрямую отслеживаем ввод пользователя
            Document.TextChanged += Document_TextChanged;

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

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath)) return;

            var folderName = new DirectoryInfo(rootPath).Name;
            if (string.IsNullOrWhiteSpace(folderName)) folderName = rootPath;

            _rootFolderItem = new FileSystemItem(folderName, rootPath, true);
            PopulateNode(_rootFolderItem);
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
            }
        }

        // Вызывается при клике на файл в TreeView
        partial void OnSelectedItemChanged(FileSystemItem? value)
        {
            if (value == null || value.IsDirectory)
            {
                Console.WriteLine("[LOG] Это папка или сброс выделения. Скрываем редактор.");
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

                    if (Document != null)
                    {
                        Document.TextChanged -= Document_TextChanged;
                    }

                    Document = new TextDocument(text);

                    // 3. Подписываемся на новый документ для автосохранения
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
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOG] ОШИБКА КАРТИНКИ: {ex.Message}");
                }
            }
            else
            {
            }
        }

        // Вызывается при наборе текста (событие TextChanged)
        private void Document_TextChanged(object? sender, EventArgs e)
        {
            if (_isLoadingFile || SelectedItem == null || !IsEditorVisible) return;

            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            _ = SaveDebouncedAsync(Document.Text, SelectedItem.Path, _saveCts.Token);
        }

        private async Task SaveDebouncedAsync(string text, string path, CancellationToken token)
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await File.WriteAllTextAsync(path, text, token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }
    }
}