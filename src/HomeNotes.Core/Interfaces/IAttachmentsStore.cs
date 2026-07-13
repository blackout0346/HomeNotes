using HomeNotes.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface IAttachmentsStore
    {
        public Task AttachmentsAddAsync(Attachments attachments);
        public Task<Attachments?> AttachmentsGetByIdAsync(Guid id);
        Task<IEnumerable<Attachments>> AttachmentsGetByNoteIdAsync(Guid noteId);
        Task AttachmentsUpdateAsync( Attachments attachments);
        public Task AttachmentsDeleteAsync(Guid id);

    }
}
