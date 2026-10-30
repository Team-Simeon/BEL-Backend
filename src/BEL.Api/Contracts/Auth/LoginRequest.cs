using System.ComponentModel.DataAnnotations;

namespace BEL.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
