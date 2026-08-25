using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Infrastucture.Data;
using HomeNotes.Core.Models;
using HomeNotes.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HomeNotes.Infrastucture.Services
{
    public class NotesStore : INotesStore
    {
        private readonly AppDbContext _appDbContext;

        public NotesStore(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }



        public async Task NotesAddAsync(Notes note)
        {
        
            note.UpdatedAt = DateTime.UtcNow;
            note.CreatedAt = DateTime.UtcNow;
            await _appDbContext.Notes.AddAsync(note);
            await _appDbContext.SaveChangesAsync();

        }

        public async Task NotesDeleteAsync(Guid id)
        {
            var note = await _appDbContext.Notes.FindAsync(id);
            if (note != null)
            {
                note.IsDeleted = true;
                note.IsSynced = false;
                note.UpdatedAt = DateTime.UtcNow;

                await _appDbContext.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Notes>> NotesGetAllAsync()
        {
            return await _appDbContext.Notes
           .Where(n => !n.IsDeleted)
           .ToListAsync();
        }

        public async Task<Notes?> NotesGetByIdAsync(Guid id)
        {
            return await _appDbContext.Notes.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }

        public async Task<IEnumerable<Notes>>NotesGetByUserIdAsync(Guid userId)
        {
            return await _appDbContext.Notes.Where(n => n.UserId == userId && !n.IsDeleted).ToListAsync();
        }

        public async Task NotesUpdateAsync(Notes note)
        {
            note.IsSynced = false;
            note.UpdatedAt = DateTime.UtcNow;
            _appDbContext.Notes.Update(note);
            await _appDbContext.SaveChangesAsync();
        }
        public async Task<IEnumerable<Notes>> NotesGetChangedSinceAsync(Guid userId, DateTime? since)
        {
            var query = _appDbContext.Notes.Where(u => u.UserId == userId);
            if(since.HasValue)
            {
                query = query.Where(n => n.UpdatedAt > since.Value);
            }
            return await query.ToListAsync();
        }

    
      
        public async Task<IEnumerable<Notes>> NotesRestoreRangeAsync(IEnumerable<Guid> noteId)
        {
            var ids = noteId.ToList();
            var notes =await _appDbContext.Notes.Include(n => n.Attachments).Where(n => ids.Contains(n.Id) && n.IsDeleted).ToListAsync(); 
            foreach(var note in notes)
            {
                note.UpdatedAt = DateTime.UtcNow;
                note.IsDeleted = false;
                note.Version++;
                note.IsSynced = false;
                foreach (var attachments in note.Attachments.Where(a => a.IsDeleted))
                {
                    attachments.IsDeleted = false;
                    attachments.UpdatedAt = DateTime.UtcNow;
                    attachments.IsSynced = false;
                }
            }
            await _appDbContext.SaveChangesAsync(); 
            return notes;

        }

        public async Task<Notes?> NotesGetByIdIncludingDeleteAsync(Guid Id)
        {
            return await _appDbContext.Notes.FirstOrDefaultAsync(n => n.Id == Id);
        }
    }
}
