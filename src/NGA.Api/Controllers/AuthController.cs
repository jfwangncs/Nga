using JfYu.WeChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NGA.Api.Extensions;
using NGA.Api.Model;
using NGA.Api.Model.Request;
using NGA.Api.Model.View;
using NGA.Api.Options;
using NGA.Api.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NGA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IJwtService jwtService, IOptions<JwtSettings> jwtSettings, IMiniProgram miniProgram) : CustomController
    {
        private readonly IJwtService _jwtService = jwtService;
        private readonly JwtSettings _jwtSettings = jwtSettings.Value;
        private readonly IMiniProgram _miniProgram = miniProgram;

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<LoginResponse>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var authSession = await _miniProgram.LoginAsync(request.Code);
#if DEBUG
            authSession = new JfYu.WeChat.Model.Response.WechatLoginResponse() { OpenId = "OpenID", SessionKey = "SessionKey" };
#endif
            if (authSession == null || !string.IsNullOrEmpty(authSession.ErrorMessage))
                return BadRequest(ErrorCode.InvalidCredentials);

            var user = new WeChatUser
            {
                Phone = request.Phone,
                OpenId = authSession.OpenId,
                SessionKey = authSession.SessionKey
            };
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.Phone),
                new Claim(ClaimTypes.Role,user.Role.GetDescription() ),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = _jwtService.GenerateToken(claims);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = request.Phone,
                ExpiresIn = _jwtSettings.Expires
            });
        }
    }
}
