using System.ComponentModel.DataAnnotations;

namespace BEL.Api.Contracts.Users;

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name must be at most 100 characters.")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name must be at most 100 characters.")]
    public string LastName { get; init; } = string.Empty;
}
