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
        public NotesService(INotesStore notesStore)
        {
            _notesStore = notesStore;
        }

        public async Task<NotesResponse> CreateNoteAsync(NotesRequest request)
        {

            var note = new Notes
            {
                Id = Guid.NewGuid(), // Генерируем ID на клиенте (важно для offline-first)
                Title = request.Title,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };
            await _notesStore.NotesAddAsync(note);
            return new NotesResponse
            {
                Id = note.Id,
                Title = note.Title,
                RelativePath = note.RelativePath,
                UpdatedAt = note.UpdatedAt
                
            };
        }

        public async Task DeleteNoteAsync(Guid id)
        {
            await _notesStore.NotesDeleteAsync(id);
        }

        public async Task<NotesResponse?> GetByIdAsync(Guid id)
        {
            var note = await _notesStore.NotesGetByIdAsync(id);
            if(note== null)
            {
                return null;
            }
            return new NotesResponse
            {
                Id = note.Id,
                Title = note.Title,
            };
        }

        public async Task<IEnumerable<NotesResponse>> GetListAsync()
        {
            var notes = await _notesStore.NotesGetAllAsync();
            return notes.Select(n => new NotesResponse
            {
                Id = n.Id,
                Title = n.Title,
                RelativePath = n.RelativePath,
                UpdatedAt = n.UpdatedAt
            });
        }

        public async Task<NotesResponse> UpdateNoteAsync(Guid id, NotesRequest request)
        {
            var note = await _notesStore.NotesGetByIdAsync(id);
            if(note == null)
            {
                throw new Exception("Note not found");
            }
            note.Title = request.Title;
            await _notesStore.NotesUpdateAsync(note);
            return new NotesResponse
            {
                Id = note.Id,
                Title = note.Title,
            };

        }
    }
}
