using BEL.Api.Models;

namespace BEL.Api.Contracts.Users;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.Role,
        user.SchoolId,
        user.School?.Name,
        user.IsApproved,
        user.IsActive,
        user.CreatedAtUtc);
}
