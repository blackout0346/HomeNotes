using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.Models;
using HomeNotes.Core.Exceptions;
namespace HomeNotes.Core.Services
{
    public class NotesService : INotesService
    {
        private readonly INotesStore _notesStore;
        private readonly IFileStore _fileStore;
        private readonly IGetUserCurrentId _getUserCurrentId;
        public NotesService(INotesStore notesStore, IFileStore fileStore, IGetUserCurrentId getUserCurrentId)
        {
            _fileStore = fileStore;
            _notesStore = notesStore;
            _getUserCurrentId = getUserCurrentId;
        }

        public async Task<NotesResponse> CreateNoteAsync(NotesRequest request)
        {
            var userId = _getUserCurrentId.UserId;
            var exists = await _notesStore.NotesGetByIdAsync(request.Id);
            if (exists != null)
            {
                if (exists.UserId != userId)
                    throw new UnauthorizedAccessException("Note id conflict.");


                return await UpdateNoteAsync(exists.Id, request);
            }
            var note = new Notes
            {
                Id = request.Id,
                Title = request.Title,
                RelativePath = request.RelativePath,
                UserId = userId,
                Version = 1,
                IsSynced = true,

            };
            await _notesStore.NotesAddAsync(note);
            if (request.Content != null)
            {
                await _fileStore.FileSaveAsync(note.RelativePath, request.Content);

            }
            return new NotesResponse
            {
                Id = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                RelativePath = note.RelativePath,
                UpdatedAt = note.UpdatedAt,
                Version = note.Version,

            };
        }

        public async Task DeleteNoteAsync(Guid id)
        {

            var note = await GetOwnedNoteOrThrowAsync(id);

            await _notesStore.NotesDeleteAsync(id);
        }

        public async Task<NotesResponse?> GetByIdAsync(Guid id)
        {
            var note = await GetOwnedNoteOrThrowAsync(id);
            return new NotesResponse
            {
                Id = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                RelativePath = note.RelativePath,
                UpdatedAt = note.UpdatedAt,
                Version = note.Version,
            };
        }

        public async Task<IEnumerable<NotesResponse>> GetListAsync()
        {

            var notes = await _notesStore.NotesGetByUserIdAsync(_getUserCurrentId.UserId);

            return notes.Select(n => new NotesResponse
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                RelativePath = n.RelativePath,
                UpdatedAt = n.UpdatedAt,
                Version = n.Version,
            });
        }

        public async Task<Notes> GetOwnedNoteOrThrowAsync(Guid id)
        {
            var userId = _getUserCurrentId.UserId;
            var note = await _notesStore.NotesGetByIdAsync(id);
            if (note == null || note.UserId != userId || note.IsDeleted)
                throw new UnauthorizedAccessException("Note not found or access denied.");

            return note;
        }

        public async Task<NotesResponse> UpdateNoteAsync(Guid id, NotesRequest request)
        {

            var note = await GetOwnedNoteOrThrowAsync(id);


            if (request.Version != note.Version)
            {
                throw new ConflictResponse("Note was updated elsewhere.", new NotesResponse
                {
                    Id = note.Id,
                    UserId = note.UserId,
                    Title = note.Title,
                    RelativePath = note.RelativePath,
                    UpdatedAt = note.UpdatedAt,
                    Version = note.Version
                }
                );
            }

            note.Title = request.Title;
            note.RelativePath = request.RelativePath;
            note.Version++;
            note.UpdatedAt = DateTime.UtcNow;
            note.IsSynced = true;


            if (request.Content != null)
            {
                await _fileStore.FileSaveAsync(note.RelativePath, request.Content);
            }

            await _notesStore.NotesUpdateAsync(note);

            return new NotesResponse
            {
                Id = note.Id,
                UserId = note.UserId,
                Title = note.Title,
                RelativePath = note.RelativePath,
                UpdatedAt = note.UpdatedAt,
                Version = note.Version
            };
        }
    }
}
