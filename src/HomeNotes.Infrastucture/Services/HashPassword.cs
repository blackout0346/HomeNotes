using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.Interfaces;
namespace HomeNotes.Infrastucture.Services
{
    internal class HashPassword : IHashPassword
    {
        const int workFactor = 12;
        public async Task<string> HashPasswordAsync(string password)
        {
            return  await Task.Run(() => BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor));
        }
       
        public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword));
        }

    }
}
