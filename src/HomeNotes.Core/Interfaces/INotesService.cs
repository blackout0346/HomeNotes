using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
namespace HomeNotes.Core.Interfaces
{
    public interface INotesService
    {
        public Task<NotesResponse> CreateNoteAsync(NotesRequest request);

        public Task<NotesResponse> UpdateNoteAsync(Guid id, NotesRequest request);

        public Task DeleteNoteAsync(Guid id);

        Task<IEnumerable<NotesResponse>> GetListAsync();

        Task<NotesResponse?> GetByIdAsync(Guid id);
        public Task<Notes> GetOwnedNoteOrThrowAsync(Guid id);
    }
}
