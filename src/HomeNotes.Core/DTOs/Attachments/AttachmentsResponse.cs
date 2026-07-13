using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Attachments
{
    public class AttachmentsResponse
    {
        public Guid Id { get; set; } 
        public Guid NoteId { get; set; }
        public string FileName { get; set; } = "";
        public long Size { get; set; }
        public string RelativePath { get; set; } = "";
        public string MimeType { get; set; } = "";

        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

    }
}
