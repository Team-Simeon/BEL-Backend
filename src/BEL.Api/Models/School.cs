namespace BEL.Api.Models;

public sealed class School
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Only users with the Teacher role are allowed to have a SchoolId (enforced by the service and a DB check constraint).
    public ICollection<User> Users { get; set; } = new List<User>();
}
