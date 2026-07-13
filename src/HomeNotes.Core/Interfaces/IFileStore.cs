using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface IFileStore
    {
        Task FileSaveAsync(string relativePath, Stream content);

        Task<Stream?> FileGetAsync(string relativePath);

        Task FileDeleteAsync(string relativePath);

        Task<bool> FileExistsAsync(string relativePath);
    }
}
