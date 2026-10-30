using BEL.Api.Contracts.Users;

namespace BEL.Api.Contracts.Auth;

public sealed record RegistrationResponse(
    UserResponse User,
    string? AccessToken,
    DateTime? ExpiresAtUtc);
