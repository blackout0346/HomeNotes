using HomeNotes.Core.DTOs.Authification;
using HomeNotes.Core.Exceptions;
using HomeNotes.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;

namespace HomeNotes.Api.Server.Controllers
{

    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), 200)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result); 
            }
            catch (ConflictResponse ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result); 
            }
            catch (AuthenticationException)
            {
      
                return Unauthorized(new { message = "Invalid login or password." });
            }
        }
    }
}