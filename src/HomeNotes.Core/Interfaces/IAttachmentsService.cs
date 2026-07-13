using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.DTOs.Attachments;
namespace HomeNotes.Core.Interfaces
{
   public interface IAttachmentsService
    {
        public Task<AttachmentsResponse?> CreateAttachAsync(AttachmentsRequest attachmentsrequest);

        public Task<AttachmentsResponse?> UpdateAttachAsync(Guid id, AttachmentsRequest attachmentsrequest);
        public Task<AttachmentsResponse?> GetAttachAsync(Guid id);
        public Task DeleteAsync(Guid id);
    }
}
