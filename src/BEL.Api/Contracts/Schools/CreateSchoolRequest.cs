using System.ComponentModel.DataAnnotations;

namespace BEL.Api.Contracts.Schools;

public sealed class CreateSchoolRequest
{
    [Required(ErrorMessage = "School name is required.")]
    [MaxLength(200, ErrorMessage = "School name must be at most 200 characters.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [MaxLength(100, ErrorMessage = "City must be at most 100 characters.")]
    public string City { get; init; } = string.Empty;

    [MaxLength(300, ErrorMessage = "Address must be at most 300 characters.")]
    public string? Address { get; init; }
}
