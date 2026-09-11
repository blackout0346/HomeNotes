using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.DTOs.Sync;
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
        Task<Stream> GetNoteContentAsync(Guid id);
        Task<SyncResponse> SyncAsync(SyncRequest request);
        public Task<Notes?> ApplySyncChangeAsync(Guid userId, NotesRequest request);
        Task UpdateNoteContentAsync(Guid id, Stream body);
        Task<IEnumerable<NotesResponse>> RestoreNotesAsync(IEnumerable<Guid> noteIds);
    }
}
