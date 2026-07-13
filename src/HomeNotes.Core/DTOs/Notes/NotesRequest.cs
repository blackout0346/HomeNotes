using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Notes
{
    public class NotesRequest
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; } = "";

        public string RelativePath { get; set; } = "";
    }
}
