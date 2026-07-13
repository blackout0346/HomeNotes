using HomeNotes.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface IAttachmentsStore
    {
        public Task<Attachments?> AttachmentsAddAsync(Attachments attachments);
        public Task<Attachments?> AttachmentsGetByIdAsync(Guid id);
        Task<Attachments?> AttachmentsUpdateAsync( Attachments attachments);
        public Task DeleteAsync(Guid id);

    }
}
