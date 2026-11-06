using System.ComponentModel.DataAnnotations.Schema;

namespace BEL.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public Guid? SchoolId { get; set; }

    public School? School { get; set; }

    // Teachers start unapproved and can sign in only after administrator approval.
    public bool IsApproved { get; set; }

    public bool IsActive { get; set; } = true;

    // Embedded in every issued JWT; rotating it invalidates all of the user's existing tokens.
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
