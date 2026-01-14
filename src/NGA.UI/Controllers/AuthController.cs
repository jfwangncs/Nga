

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NGA.UI.Extensions;
using NGA.UI.Model.Request;
using NGA.UI.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NGA.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : CustomController
    {
        private readonly IJwtService _jwtService;

        public AuthController(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request.Phone != "admin" || request.Code != "password")
            {
                return BadRequest(ErrorCode.InvalidCredentials);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.Phone),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = _jwtService.GenerateToken(claims);

            return Ok(new
            {
                Token = token,
                Username = request.Phone,
                ExpiresIn = 3600
            });
        }

        [HttpPost("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                Username = username,
                Role = role,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }
    }
}
