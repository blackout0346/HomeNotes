using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Infrastucture.Data;
using HomeNotes.Core.Models;
using HomeNotes.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HomeNotes.Infrastucture.Services
{
    public class NotesService : INotesStore
    {
        private readonly AppDbContext _appDbContext;

        public NotesService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }



        public async Task NotesAddAsync(Notes note)
        {
            note.IsSynced = false;
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

        public async Task<IEnumerable<Notes?>>NotesGetByUserIdAsync(Guid userId)
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
    }
}
