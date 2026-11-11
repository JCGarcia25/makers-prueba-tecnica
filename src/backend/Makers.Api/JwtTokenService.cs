using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Makers.Domain;
using Microsoft.IdentityModel.Tokens;

namespace Makers.Api;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public TokenResponse Create(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("security_version", user.SecurityVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no está configurado.")));
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: expiresAt.UtcDateTime, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt,
            new CurrentUser(user.Id, user.Email, user.Name, user.Role));
    }
}
