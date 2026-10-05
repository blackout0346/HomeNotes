using System.Threading.Tasks;

namespace HomeNotes.Desktop.Interface;

public interface IDialogService
{
    public Task<string> SelectFolderAsync();
}