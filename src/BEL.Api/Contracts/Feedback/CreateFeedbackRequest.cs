using System.ComponentModel.DataAnnotations;
using BEL.Api.Models;

namespace BEL.Api.Contracts.Feedback;

public sealed class CreateFeedbackRequest : IValidatableObject
{
    public string? Type { get; init; }

    public string? Message { get; init; }

    public FeedbackType? GetFeedbackType() =>
        Enum.TryParse<FeedbackType>(Type, ignoreCase: false, out var type) && Enum.IsDefined(type)
            ? type
            : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            yield return new ValidationResult(
                "Feedback type is required.",
                [nameof(Type)]);
        }
        else if (GetFeedbackType() is null)
        {
            yield return new ValidationResult(
                "Feedback type must be either Problem or Suggestion.",
                [nameof(Type)]);
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            yield return new ValidationResult(
                "Message is required.",
                [nameof(Message)]);
        }
        else if (Message.Trim().Length is < Models.Feedback.MinMessageLength or > Models.Feedback.MaxMessageLength)
        {
            yield return new ValidationResult(
                $"Message must be between {Models.Feedback.MinMessageLength} and {Models.Feedback.MaxMessageLength} characters after trimming spaces.",
                [nameof(Message)]);
        }
    }
}
