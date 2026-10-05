using System.Collections.ObjectModel;

namespace HomeNotes.Desktop.Services;

public record class FileSystemItem(string Name, string Path, bool IsDirectory)
{
   
    public ObservableCollection<FileSystemItem>? SubItems { get; } = IsDirectory ? new() : null;


    public string DisplayName => IsDirectory ? Name : System.IO.Path.GetFileNameWithoutExtension(Name);


    public string ExtensionTag
    {
        get
        {
            if (IsDirectory) return "";
            var ext = System.IO.Path.GetExtension(Name).TrimStart('.').ToUpper();
            return ext == "MD" ? "" : ext;
        }
    }
}