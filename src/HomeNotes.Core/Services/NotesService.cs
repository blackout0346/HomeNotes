using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.Models;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.DTOs.Sync;
using System.Diagnostics;
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
            return MapToResponse(note);
        }

        public async Task DeleteNoteAsync(Guid id)
        {

            var note = await GetOwnedNoteOrThrowAsync(id);

            await _notesStore.NotesDeleteAsync(id);
        }

        public async Task<NotesResponse?> GetByIdAsync(Guid id)
        {
            var note = await GetOwnedNoteOrThrowAsync(id);
            return MapToResponse(note);
        }

        public async Task<IEnumerable<NotesResponse>> GetListAsync()
        {

            var notes = await _notesStore.NotesGetByUserIdAsync(_getUserCurrentId.UserId);

            return notes.Select(MapToResponse);
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

            return MapToResponse(note);

        }
        public async Task<Stream> GetNoteContentAsync(Guid id)
        {
            var note = await GetOwnedNoteOrThrowAsync(id);
            var stream = await _fileStore.FileGetAsync(note.RelativePath);
            if (stream == null)
                throw new FileNotFoundException("Note content file not found.");
            return stream;
        }
        public async Task UpdateNoteContentAsync(Guid id, Stream content)
        {
            var note = GetOwnedNoteOrThrowAsync(id);
            await _fileStore.FileSaveAsync(note.Result.RelativePath, content);
            note.Result.UpdatedAt = DateTime.UtcNow;
            note.Result.Version++;
            note.Result.IsSynced = true;
            await _notesStore.NotesUpdateAsync(note.Result);
        }


        public async Task<SyncResponse> SyncAsync(SyncRequest request)
        {
            var userId = _getUserCurrentId.UserId;
            var response = new SyncResponse();

            foreach (var noteRequest in request.ChangedNotes)
            {
                try
                {
                    
                    var conflictCopy = await ApplySyncChangeAsync(userId, noteRequest);

                    if (conflictCopy != null)
                    {
       
                        response.Conflicts.Add(MapToResponse(conflictCopy));
                    }
                }
                catch (UnauthorizedAccessException)
                {
        
                }
            }

            foreach (var deletedId in request.DeletedNoteIds)
            {
                try
                {
                    await DeleteNoteAsync(deletedId);
                }
                catch (UnauthorizedAccessException)
                {
                
                }
            }

       
            response.ServerTime = DateTime.UtcNow;

    
            var serverChanges = await _notesStore.NotesGetChangedSinceAsync(userId, request.Since);
            response.ServerChanges = serverChanges.Select(MapToResponse).ToList();

            return response;
        }
       public async Task<Notes?> ApplySyncChangeAsync(Guid userId, NotesRequest request)
        {
            var existing = await _notesStore.NotesGetByIdAsync(request.Id);

            if (existing == null)
            {

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
                return null;
            }

            if (existing.UserId != userId)
                throw new UnauthorizedAccessException("Note id conflict.");

            if (existing.IsDeleted)
                throw new UnauthorizedAccessException("Note was deleted.");

            if (request.Version != existing.Version)
            {

                var conflictCopy = new Notes
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = $"{request.Title} (конфликт от {DateTime.UtcNow:dd.MM.yyyy HH:mm})",
                    RelativePath = $"{userId}/{Guid.NewGuid()}.md",
                    Version = 1,
                    IsSynced = true,
                };
                await _notesStore.NotesAddAsync(conflictCopy);

                if (request.Content != null)
                {
                    await _fileStore.FileSaveAsync(conflictCopy.RelativePath, request.Content);
                }

                return conflictCopy;
            }

            existing.Title = request.Title;
            existing.RelativePath = request.RelativePath;
            existing.Version++;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsSynced = true;

            await _notesStore.NotesUpdateAsync(existing);
            return null;
        }
        
        private static NotesResponse MapToResponse(Notes note) => new()
        {
            Id = note.Id,
            UserId = note.UserId,
            Title = note.Title,
            RelativePath = note.RelativePath,
            UpdatedAt = note.UpdatedAt,
            Version = note.Version,
            IsDeleted = note.IsDeleted
        };

        public async Task<IEnumerable<NotesResponse>> RestoreNotesAsync(IEnumerable<Guid> noteIds)
        {
            var currentuser = _getUserCurrentId.UserId;
            var ownedId = new List<Guid>();
            foreach (var noteId in noteIds)
            {
                var note = await _notesStore.NotesGetByIdIncludingDeleteAsync(noteId);
                if(note != null && note.UserId == currentuser)
                {
                    ownedId.Add(noteId);
                }

            }
            var restore = await _notesStore.NotesRestoreRangeAsync(ownedId);
            return restore.Select(MapToResponse);
        }

    
    }
}
