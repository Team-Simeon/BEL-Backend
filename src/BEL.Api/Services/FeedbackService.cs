using System.Data.Common;
using BEL.Api.Contracts.Feedback;
using BEL.Api.Data;
using BEL.Api.Exceptions;
using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Services;

public sealed class FeedbackService(BelDbContext db) : IFeedbackService
{
    public async Task<FeedbackResponse> CreateAsync(
        Guid userId,
        CreateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var type = request.GetFeedbackType()
            ?? throw new BadRequestException("Feedback type must be either Problem or Suggestion.");

        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length is < Feedback.MinMessageLength or > Feedback.MaxMessageLength)
        {
            throw new BadRequestException(
                $"Message must be between {Feedback.MinMessageLength} and {Feedback.MaxMessageLength} characters after trimming spaces.");
        }

        var feedback = new Feedback
        {
            Type = type,
            Message = message,
            UserId = userId
        };

        db.Feedback.Add(feedback);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is DbException)
        {
            throw new DatabaseUnavailableException(
                "The database is temporarily unavailable. Please try again later.",
                exception);
        }

        return feedback.ToResponse();
    }

    public async Task<IReadOnlyList<FeedbackResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Feedback
            .AsNoTracking()
            .OrderByDescending(feedback => feedback.CreatedAtUtc)
            .ToResponse()
            .ToListAsync(cancellationToken);
    }
}
