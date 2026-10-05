using System.Collections.ObjectModel;

namespace HomeNotes.Desktop.Services;

public record class FileSystemItem(string Name, string Path, bool IsDirectory)
{
    public ObservableCollection<FileSystemItem>? SubItems { get; } = IsDirectory ? new() : null;
}