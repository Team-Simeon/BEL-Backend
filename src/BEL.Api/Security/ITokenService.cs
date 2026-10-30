using BEL.Api.Models;

namespace BEL.Api.Security;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
}
