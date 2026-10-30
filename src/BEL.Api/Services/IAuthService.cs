using BEL.Api.Contracts.Auth;

namespace BEL.Api.Services;

public interface IAuthService
{
    Task<RegistrationResponse> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken cancellationToken);

    Task<RegistrationResponse> RegisterTeacherAsync(RegisterTeacherRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, string tokenId, DateTime expiresAtUtc, CancellationToken cancellationToken);

    Task LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken);
}
