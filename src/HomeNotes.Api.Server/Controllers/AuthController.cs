using HomeNotes.Core.DTOs.Authification;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;

namespace HomeNotes.Api.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result); // содержит Token, Login, UserId
            }
            catch (ConflictResponse ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result); // содержит Token, Login, UserId
            }
            catch (AuthenticationException)
            {
                // намеренно не уточняем, что именно неверно — логин или пароль,
                // чтобы не давать подсказок для перебора
                return Unauthorized(new { message = "Invalid login or password." });
            }
        }
    }
}