namespace BEL.Api.Models;

public sealed class Feedback
{
    public const int MinMessageLength = 10;

    public const int MaxMessageLength = 1000;

    public Guid Id { get; set; } = Guid.NewGuid();

    public FeedbackType Type { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
