using HomeNotes.Core.DTOs.Authification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
namespace HomeNotes.Desktop.Services
{
    public class AuthClientService
    {
        readonly HttpClient _httpClient;
        public AuthClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;

        }
        public async Task<RegisterResponse> Register(string login, string password)
        {
            var registerRequest = new RegisterRequest
            {
                Login = login,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", registerRequest);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RegisterResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API Error: {errorContent}");
                return null;
            }

        }
        public async Task<LoginResponse> Login(string login, string password)
        {
            var loginRequest = new LoginRequest
            {
                Login = login,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest); 
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponse>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API Error: {error}");
                return null;
            }

        }

    }
}
