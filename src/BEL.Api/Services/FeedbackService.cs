using BEL.Api.Contracts.Feedback;
using BEL.Api.Data;
using BEL.Api.Exceptions;
using BEL.Api.Models;

namespace BEL.Api.Services;

public sealed class FeedbackService(BelDbContext db) : IFeedbackService
{
    public async Task<FeedbackResponse> CreateAsync(
        Guid userId,
        CreateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var type = request.Type
            ?? throw new BadRequestException("Feedback type is required.");

        var message = request.Message.Trim();
        if (message.Length is < Feedback.MinMessageLength or > Feedback.MaxMessageLength)
        {
            throw new BadRequestException("Message must be between 10 and 1000 characters.");
        }

        var feedback = new Feedback
        {
            Type = type,
            Message = message,
            UserId = userId
        };

        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);

        return feedback.ToResponse();
    }
}
