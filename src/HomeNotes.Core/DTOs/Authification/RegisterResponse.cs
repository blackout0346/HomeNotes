using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.DTOs.Authification
{
    public class RegisterResponse
    {
        public Guid UserId { get; set; }
        public string Login { get; set; } = string.Empty;
    }
}
