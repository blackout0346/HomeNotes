using HomeNotes.Core.DTOs.Attachments;
using HomeNotes.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Services
{
    internal class AttachmentsService : IAttachmentsService
    {
        public Task<AttachmentsResponse?> CreateAttachAsync(AttachmentsRequest attachmentsrequest)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<AttachmentsResponse?> GetAttachAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<AttachmentsResponse?> UpdateAttachAsync(Guid id, AttachmentsRequest attachmentsrequest)
        {
            throw new NotImplementedException();
        }
    }
}
