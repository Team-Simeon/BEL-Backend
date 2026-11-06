using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BEL.Api.Exceptions;

namespace BEL.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedException("The authenticated user identifier is missing.");
    }

    public static string GetTokenId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Jti) is { Length: > 0 } tokenId
            ? tokenId
            : throw new UnauthorizedException("The access token identifier is missing.");

    public static DateTime GetTokenExpiresAtUtc(this ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Exp), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : throw new UnauthorizedException("The access token expiry is missing.");

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(nameof(Models.UserRole.Admin));
}
