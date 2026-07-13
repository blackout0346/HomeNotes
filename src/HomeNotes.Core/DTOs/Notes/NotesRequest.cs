using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Notes
{
    public class NotesRequest : IDisposable
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public Stream Content { get; set; } = null!;
        public int Version { get; set; }
        public void Dispose()
        {
            Content?.Dispose();
        }
    }
}
