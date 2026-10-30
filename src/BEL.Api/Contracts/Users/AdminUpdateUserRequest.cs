using System.ComponentModel.DataAnnotations;
using BEL.Api.Models;

namespace BEL.Api.Contracts.Users;

public sealed class AdminUpdateUserRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name must be at most 100 characters.")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name must be at most 100 characters.")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public UserRole? Role { get; init; }

    public Guid? SchoolId { get; init; }

    public bool IsApproved { get; init; }

    public bool IsActive { get; init; } = true;
}
