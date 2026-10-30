using BEL.Api.Models;

namespace BEL.Api.Contracts.Users;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? SchoolId,
    string? SchoolName,
    bool IsApproved,
    bool IsActive,
    DateTime CreatedAtUtc);
