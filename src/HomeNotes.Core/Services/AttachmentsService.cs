using HomeNotes.Core.DTOs.Attachments;
using HomeNotes.Core.Interfaces;
using HomeNotes.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace HomeNotes.Core.Services
{
    public class AttachmentsService : IAttachmentsService
    {
        private readonly IAttachmentsStore _attachmentsStore;
        private readonly IFileStore _fileStore;
        private readonly INotesService _notesService;
        public AttachmentsService(IAttachmentsStore attachmentsStore, IFileStore fileStore,INotesService notesService)
        {
            _attachmentsStore = attachmentsStore;
            _fileStore = fileStore;
            _notesService = notesService;
        }
        public async Task<AttachmentsResponse?> CreateAttachAsync(AttachmentsRequest attachmentsrequest)
        {
            var note = await _notesService.GetOwnedNoteOrThrowAsync(attachmentsrequest.NotesId);

            var attachment = new Attachments()
            {
                Id = Guid.NewGuid(),
                FileName = attachmentsrequest.FileName,
                MimeType = attachmentsrequest.MimeType,
                NotesId = attachmentsrequest.NotesId,
                RelativePath = $"{note.UserId}/{note.Id}/{Guid.NewGuid()}_{attachmentsrequest.FileName}",
                Size = attachmentsrequest.Size,
                IsSynced = false,

            };

            await _fileStore.FileSaveAsync(attachment.RelativePath, attachmentsrequest.FileStream);
            attachment.IsSynced = true;
            await _attachmentsStore.AttachmentsAddAsync(attachment);
            return new AttachmentsResponse
            {
                Id = attachment.Id,
                NoteId = attachment.NotesId,
                FileName = attachment.FileName,
                Size = attachment.Size,
                RelativePath = attachment.RelativePath,
                MimeType = attachment.MimeType,
                UpdatedAt = attachment.UpdatedAt,
                IsDeleted = attachment.IsDeleted
            };
        }

        public async Task DeleteAsync(Guid id)
        {
            await GetOwnedAttachmentOrThrowAsync(id);
            await _attachmentsStore.AttachmentsDeleteAsync(id);
        }

        public async Task<AttachmentsResponse?> GetAttachAsync(Guid id)
        {
            var attachment = await GetOwnedAttachmentOrThrowAsync(id);
            return new AttachmentsResponse
            {
                Id = attachment.Id,
                NoteId = attachment.NotesId,
                FileName = attachment.FileName,
                Size = attachment.Size,
                RelativePath = attachment.RelativePath,
                MimeType = attachment.MimeType,
                UpdatedAt = attachment.UpdatedAt,
                IsDeleted = attachment.IsDeleted

            };
        }

        public async Task<AttachmentsResponse?> UpdateAttachAsync(Guid id, AttachmentsRequest attachmentsrequest)
        {
            var attachment = await GetOwnedAttachmentOrThrowAsync(id);
            attachment.FileName = attachmentsrequest.FileName;
            attachment.MimeType = attachmentsrequest.MimeType;
            attachment.Size = attachmentsrequest.Size;
            attachment.UpdatedAt = DateTime.UtcNow;
            attachment.IsSynced = false;
            if (attachmentsrequest.FileStream != null && attachmentsrequest.FileStream != Stream.Null)
            {
                await _fileStore.FileSaveAsync(attachment.RelativePath, attachmentsrequest.FileStream);
            }
            var updated = await _attachmentsStore.AttachmentsUpdateAsync(attachment);

            if (updated == null) return null;

            return new AttachmentsResponse
            {
                Id = attachment.Id,
                NoteId = attachment.NotesId,
                FileName = attachment.FileName,
                Size = attachment.Size,
                RelativePath = attachment.RelativePath,
                MimeType = attachment.MimeType,
                UpdatedAt = attachment.UpdatedAt,
                IsDeleted = attachment.IsDeleted
            };
        }
        private async Task<Attachments> GetOwnedAttachmentOrThrowAsync(Guid attachmentId)
        {
            var attachment = await _attachmentsStore.AttachmentsGetByIdAsync(attachmentId);
            if (attachment == null || attachment.IsDeleted)
                throw new UnauthorizedAccessException("Attachment not found.");

            await _notesService.GetOwnedNoteOrThrowAsync(attachment.NotesId);
            return attachment;
        }


    }
}
