using HomeNotes.Core.DTOs.Authification;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    internal interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequest registerRequest);
        Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
    }
}
