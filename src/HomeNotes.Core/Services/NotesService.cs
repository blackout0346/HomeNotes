using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.Models;
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
                UpdatedAt = note.UpdatedAt

            };
        }

        public async Task DeleteNoteAsync(Guid id)
        {
            var userId = _getUserCurrentId.UserId;
            var note = await _notesStore.NotesGetByIdAsync(id);
            if (note == null || note.UserId != userId)
            {
                throw new UnauthorizedAccessException("Note not found or access denied.");
            }
            await _notesStore.NotesDeleteAsync(id);
        }

        public async Task<NotesResponse?> GetByIdAsync(Guid id)
        {
            var userId = _getUserCurrentId.UserId;
            var note = await _notesStore.NotesGetByIdAsync(id);

            if (note == null || note.UserId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to access this note.");
            }
            return new NotesResponse
            {
                Id = note.Id,
                Title = note.Title,
            };
        }

        public async Task<IEnumerable<NotesResponse>> GetListAsync()
        {

            var userId = _getUserCurrentId.UserId;
            var notes = await _notesStore.NotesGetByUserIdAsync(userId);

            return notes.Select(n => new NotesResponse
            {
                Id = n.Id,
                Title = n.Title,
                RelativePath = n.RelativePath,
                UpdatedAt = n.UpdatedAt
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
            // 1. Достаём заметку и сразу проверяем, что она принадлежит текущему юзеру
            var note = await GetOwnedNoteOrThrowAsync(id);

            // 2. Проверка конфликта версий (offline-sync)
            if (request.Version != note.Version)
            {
                return null;
            }

            // 3. Обновляем метаданные
            note.Title = request.Title;
            note.RelativePath = request.RelativePath;
            note.Version++;
            note.UpdatedAt = DateTime.UtcNow;
            note.IsSynced = true;

            // 4. Обновляем файл контента, если он был передан
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
