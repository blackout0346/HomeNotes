using HomeNotes.Core.DTOs.Notes;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Sync
{
    public class SyncResponse
    {
        public DateTime ServerTime { get; set; } 
        public List<NotesResponse> ServerChanges { get; set; } = []; 
        public List<NotesResponse> Conflicts { get; set; } = [];
    }
}
