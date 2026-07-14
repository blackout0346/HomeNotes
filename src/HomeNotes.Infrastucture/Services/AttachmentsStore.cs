using HomeNotes.Core.Interfaces;
using HomeNotes.Core.Models;
using HomeNotes.Infrastucture.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Infrastucture.Services
{
    public class AttachmentsStore : IAttachmentsStore
    {
        public AppDbContext _appdbcontext;
        public AttachmentsStore(AppDbContext appDbContext)
        {
            _appdbcontext = appDbContext;
        }
        public async Task AttachmentsAddAsync(Attachments attachments)
        {
            await _appdbcontext.Attachments.AddAsync(attachments);
            await _appdbcontext.SaveChangesAsync();
    
        }

        public async Task<Attachments?> AttachmentsGetByIdAsync(Guid id)
        {
            return await _appdbcontext.Attachments.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        }

        public async Task<IEnumerable<Attachments>> AttachmentsGetByNoteIdAsync(Guid noteId)
        {
            return await _appdbcontext.Attachments.Where(p=> p.NotesId == noteId && !p.IsDeleted).ToListAsync();
        }

        public async Task<Attachments?> AttachmentsUpdateAsync(Attachments attachments)
        {
            var existing = await _appdbcontext.Attachments.FindAsync(attachments.Id);
            if (existing == null)
            {
                return null;
            }
            existing.FileName = attachments.FileName;
            existing.MimeType = attachments.MimeType;
            existing.Size = attachments.Size;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsSynced = false;
            await _appdbcontext.SaveChangesAsync();
            return existing;
        }

        public async Task AttachmentsDeleteAsync(Guid id)
        {
            var attachments= await _appdbcontext.Attachments.FindAsync(id);
            if (attachments != null)
            { 
                attachments.IsDeleted = true;
                attachments.IsSynced = false;
                attachments.UpdatedAt = DateTime.UtcNow;
               await _appdbcontext.SaveChangesAsync();
            }

        }
    }
}
