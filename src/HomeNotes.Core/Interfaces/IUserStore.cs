using HomeNotes.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface IUserStore
    {
        Task<User?> GetByLoginAsync(string login);
        Task<User?> GetByIdAsync(Guid id);
        Task AddAsync(User user);
        Task<bool> ExistsByLoginAsync(string login);
    }
}
