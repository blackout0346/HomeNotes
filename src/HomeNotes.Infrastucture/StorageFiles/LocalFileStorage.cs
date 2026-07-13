using HomeNotes.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Infrastucture.StorageFiles
{

    public class LocalFileStorage :IFileStore
    {
        private readonly string _rootPath;
        public LocalFileStorage(string rootPath)
        {
            _rootPath = Path.Combine(rootPath, "Storage");
        }

        public Task FileDeleteAsync(string relativePath)
        {
            var fullpath = Path.Combine(_rootPath, relativePath);
            if (File.Exists(fullpath))
            {
                File.Delete(fullpath);
            }
            return Task.CompletedTask;

        }

        public Task<bool> FileExistsAsync(string relativePath)
        {
            var fullpath = Path.Combine(_rootPath, relativePath);
            return Task.FromResult(File.Exists(fullpath));
        }

        public Task<Stream?> FileGetAsync(string relativePath)
        {
            var fullpath = Path.Combine(_rootPath, relativePath);
            if (!File.Exists(fullpath))
            {
                return Task.FromResult<Stream?>(null);
            }
            Stream stream =new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return Task.FromResult<Stream?>(stream);
        }

        public async Task FileSaveAsync(string relativePath, Stream content)
        {
            var fullpath= Path.Combine(_rootPath, relativePath);
            var directoryPath = Path.GetDirectoryName(fullpath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath!);
            }
            await using var filestream = new FileStream(fullpath, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(filestream);
        }
    }
}
