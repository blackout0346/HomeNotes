using HomeNotes.Core.Interfaces;
using HomeNotes.Core.Models;
using HomeNotes.Infrastucture.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Infrastucture.Services
{
    public class AttachmentsService : IAttachmentsStore
    {
        public AppDbContext _appdbcontext;
        public AttachmentsService(AppDbContext appDbContext)
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
            return await _appdbcontext.Attachments.FirstOrDefaultAsync(f => f.Id == id);

        }

        public async Task<IEnumerable<Attachments>> AttachmentsGetByNoteIdAsync(Guid noteId)
        {
            return await _appdbcontext.Attachments.Where(p=> p.NotesId == noteId).ToListAsync();
        }

        public async Task AttachmentsUpdateAsync(Attachments attachments)
        {
        
            await _appdbcontext.SaveChangesAsync();
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
