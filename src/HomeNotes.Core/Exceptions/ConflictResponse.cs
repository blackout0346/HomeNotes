using HomeNotes.Core.DTOs.Notes;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Exceptions
{
    public class ConflictResponse : Exception
    {
        public NotesResponse? ServerNote { get; }

        public ConflictResponse(string message, NotesResponse? serverNote = null) : base(message)
        {
            ServerNote = serverNote;
        }
    }
}
