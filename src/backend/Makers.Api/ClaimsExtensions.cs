using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Makers.Api;

public static class ClaimsExtensions
{
    public static Guid UserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
}
