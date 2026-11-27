using BEL.Api.Models;

namespace BEL.Api.Contracts.Feedback;

public sealed record FeedbackResponse(
    Guid Id,
    FeedbackType Type,
    string Message,
    DateTime CreatedAtUtc);
