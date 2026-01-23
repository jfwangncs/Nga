using System.Security.Claims;

namespace NGA.Api.Services.Interfaces
{
    public interface IJwtService
    { 
        string GenerateToken(IEnumerable<Claim> claims);
        ClaimsPrincipal ValidateToken(string token);
    }
}
