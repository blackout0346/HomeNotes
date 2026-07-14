using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Authification
{
    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Token { get; set; }
    }
}
