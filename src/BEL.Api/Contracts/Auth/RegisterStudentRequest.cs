using System.ComponentModel.DataAnnotations;

namespace BEL.Api.Contracts.Auth;

public sealed class RegisterStudentRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [MaxLength(256, ErrorMessage = "Email must be at most 256 characters.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name must be at most 100 characters.")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name must be at most 100 characters.")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(
        @"^(?=.*[A-Za-z])(?=.*\d).{8,128}$",
        ErrorMessage = "Password must be 8-128 characters long and contain at least one letter and one digit.")]
    public string Password { get; init; } = string.Empty;
}
