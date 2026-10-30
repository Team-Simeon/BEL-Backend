using BEL.Api.Models;

namespace BEL.Api.Contracts.Users;

public sealed class UserQuery
{
    public UserRole? Role { get; init; }

    public Guid? SchoolId { get; init; }

    public bool? IsApproved { get; init; }

    public bool? IsActive { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
