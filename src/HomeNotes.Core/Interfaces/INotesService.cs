using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.DTOs.Notes;
namespace HomeNotes.Core.Interfaces
{
    interface INotesService
    {
        public Task<NotesResponse> CreateNoteAsync(NotesRequest request);

        public Task<NotesResponse> UpdateNoteAsync( Guid id,NotesRequest request);

        public Task DeleteNoteAsync(Guid id);

        Task<IEnumerable<NotesResponse>> GetListAsync();

        Task<NotesResponse?> GetByIdAsync(Guid id);
    }
}
