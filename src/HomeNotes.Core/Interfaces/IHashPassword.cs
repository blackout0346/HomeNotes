using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface IHashPassword
    {
        public Task<string> HashPasswordAsync(string password);

        public Task<bool> VerifyPasswordAsync(string password, string hashedPassword);

    }
}
