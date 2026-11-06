using System.Security.Claims;

namespace BEL.Api.Security;

public interface ITokenSessionValidator
{
    // Returns null when the token's session is still valid, otherwise the reason it was rejected.
    Task<string?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
