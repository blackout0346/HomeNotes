using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Attachments
{
    public class AttachmentsRequest : IDisposable
    {
        public Guid NotesId { get; set; }
        public string FileName { get; set; } = "";
        public long Size { get; set; }
        public string RelativePath { get; set; } = "";
        public string MimeType { get; set; } = "";
        public Stream FileStream { get; set; } = Stream.Null;
        public void Dispose()
        {
            FileStream?.Dispose();
        }
    }
}
