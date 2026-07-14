using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(Guid userId, string login);
    }
}
