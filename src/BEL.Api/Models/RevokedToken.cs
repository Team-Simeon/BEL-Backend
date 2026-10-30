namespace BEL.Api.Models;

// A single access token (identified by its jti claim) that was explicitly logged out before it expired.
public sealed class RevokedToken
{
    public string TokenId { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime RevokedAtUtc { get; set; } = DateTime.UtcNow;
}
