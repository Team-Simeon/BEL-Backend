namespace BEL.Api.Contracts.Feedback;

public static class FeedbackMappings
{
    public static FeedbackResponse ToResponse(this Models.Feedback feedback) => new(
        feedback.Id,
        feedback.Type,
        feedback.Message,
        feedback.CreatedAtUtc);

    public static IQueryable<FeedbackResponse> ToResponse(this IQueryable<Models.Feedback> feedback) =>
        feedback.Select(entry => new FeedbackResponse(
            entry.Id,
            entry.Type,
            entry.Message,
            entry.CreatedAtUtc));
}
