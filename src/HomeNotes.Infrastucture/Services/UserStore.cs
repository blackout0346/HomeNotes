using HomeNotes.Core.Interfaces;
using HomeNotes.Core.Models;
using HomeNotes.Infrastucture.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Infrastucture.Services
{
    public class UserStore : IUserStore
    {

        public readonly AppDbContext _appDbContext;
        public UserStore(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task AddAsync(User user)
        {
            await _appDbContext.User.AddAsync(user);
            await _appDbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsByLoginAsync(string login)
        {
            return await _appDbContext.User.AnyAsync(u=>u.Login == login);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _appDbContext.User.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            return await _appDbContext.User.FirstOrDefaultAsync(u => u.Login == login);
        }
    }
}
