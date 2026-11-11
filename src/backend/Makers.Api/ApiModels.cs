using System.ComponentModel.DataAnnotations;

namespace Makers.Api;

public sealed record LoginRequest(
    [property: Required, EmailAddress, MaxLength(254)] string Email,
    [property: Required, MinLength(10), MaxLength(128)] string Password);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUser User);
public sealed record CurrentUser(Guid Id, string Email, string Name, string Role);
