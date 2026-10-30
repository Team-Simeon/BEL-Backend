using BEL.Api.Contracts.Users;

namespace BEL.Api.Services;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<UserResponse>> ListAsync(UserQuery query, CancellationToken cancellationToken);

    Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);

    Task<UserResponse> AdminUpdateAsync(
        Guid targetUserId,
        Guid adminUserId,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken);

    Task<UserResponse> ApproveTeacherAsync(Guid targetUserId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken);
}
