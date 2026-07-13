using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Notes
{
    public class NotesResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public string Title { get; set; } = "";

        public string RelativePath { get; set; } = "";

        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
        public bool IsDeleted { get; set; }
    }
}
