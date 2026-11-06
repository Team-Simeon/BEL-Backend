using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BEL.Api.Data;
using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Security;

// Runs after the JWT signature and lifetime checks so that logout, deactivation, deletion and
// role changes take effect immediately instead of when the token expires.
public sealed class TokenSessionValidator(BelDbContext db) : ITokenSessionValidator
{
    public async Task<string?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !Guid.TryParse(principal.FindFirstValue(BelClaimTypes.SecurityStamp), out var securityStamp) ||
            principal.FindFirstValue(JwtRegisteredClaimNames.Jti) is not { Length: > 0 } tokenId)
        {
            return "The token is missing required claims.";
        }

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.SecurityStamp, u.Role, u.IsActive, u.IsApproved })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return "The account no longer exists.";
        }

        if (user.SecurityStamp != securityStamp ||
            !principal.IsInRole(user.Role.ToString()))
        {
            return "The session has ended. Please sign in again.";
        }

        if (!user.IsActive)
        {
            return "This account is deactivated.";
        }

        if (user.Role == UserRole.Teacher && !user.IsApproved)
        {
            return "Teacher accounts must be approved by an administrator.";
        }

        var revoked = await db.RevokedTokens.AnyAsync(t => t.TokenId == tokenId, cancellationToken);
        return revoked ? "The token has been revoked." : null;
    }
}
