using System.ComponentModel.DataAnnotations;
using BEL.Api.Models;

namespace BEL.Api.Contracts.Feedback;

public sealed class CreateFeedbackRequest
{
    [Required(ErrorMessage = "Feedback type is required.")]
    public FeedbackType? Type { get; init; }

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(
        Models.Feedback.MaxMessageLength,
        MinimumLength = Models.Feedback.MinMessageLength,
        ErrorMessage = "Message must be between 10 and 1000 characters.")]
    public string Message { get; init; } = string.Empty;
}
