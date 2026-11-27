namespace BEL.Api.Contracts.Feedback;

public static class FeedbackMappings
{
    public static FeedbackResponse ToResponse(this Models.Feedback feedback) => new(
        feedback.Id,
        feedback.Type,
        feedback.Message,
        feedback.CreatedAtUtc);
}
