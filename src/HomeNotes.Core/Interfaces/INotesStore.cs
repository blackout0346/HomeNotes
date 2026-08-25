using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.Models;
namespace HomeNotes.Core.Interfaces
{
    public interface INotesStore
    {
        Task NotesAddAsync(Notes note);

        Task<Notes?> NotesGetByIdAsync(Guid id);

        Task<IEnumerable<Notes>> NotesGetAllAsync();

        Task NotesUpdateAsync(Notes note);

        Task NotesDeleteAsync(Guid id);
        Task<IEnumerable<Notes?>>NotesGetByUserIdAsync(Guid userId);
        Task<IEnumerable<Notes>> NotesGetChangedSinceAsync(Guid userId, DateTime? since);
        Task<IEnumerable<Notes>> NotesRestoreRangeAsync(IEnumerable<Guid> noteId);
        Task<Notes?> NotesGetByIdIncludingDeleteAsync(Guid Id);
    }
}
