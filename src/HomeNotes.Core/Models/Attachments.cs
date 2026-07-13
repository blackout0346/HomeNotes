using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text;

namespace HomeNotes.Core.Models
{
    public class Attachments
    {
        [Key]
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public long Size { get; set; }
        public string RelativePath { get; set; } = "";
        public string MimeType { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid NotesId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSynced { get; set; }
        public Notes Notes { get; set; }
    }

}
