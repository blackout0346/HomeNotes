using HomeNotes.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using HomeNotes.Core.Models;
using HomeNotes.Core.DTOs.Authification;

namespace HomeNotes.Core.Services
{
    public class AuthService : IAuthService
    {
   
        private readonly IHashPassword _hashPassword;
        private readonly IUserStore _userStore;
        public AuthService(IHashPassword hashPassword, IUserStore userStore)
        {
            _hashPassword = hashPassword;
            _userStore = userStore;
        }
        async Task<RegisterResponse> IAuthService.RegisterAsync(RegisterRequest registerRequest)
        {
            var exists = await _userStore.ExistsByLoginAsync(registerRequest.Login);
            if (exists)
            {
                throw new Exception("User already exists.");
            }

            var hashpassword = await _hashPassword.HashPasswordAsync(registerRequest.Password);
       
            var user = new User
            {
                Login = registerRequest.Login,
                HashPassword = hashpassword
            };
            await _userStore.AddAsync(user);

            return new RegisterResponse
            {
                Login = user.Login,
                UserId = user.Id,
            };

        }
        async Task<LoginResponse> IAuthService.LoginAsync(LoginRequest loginRequest)
        {
            var user = await _userStore.GetByLoginAsync(loginRequest.Login);
            if (user == null)
            {
                throw new Exception("User not found.");
            }
            var hashpaswword =await _hashPassword.VerifyPasswordAsync(loginRequest.Password, user.HashPassword);

            if(!hashpaswword)
            {
                throw new Exception("Invalid login or password.");
            }

            return new LoginResponse
            {
                Login = user.Login,
                UserId = user.Id,
            };
        }

    }
}
