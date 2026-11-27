using BEL.Api.Contracts.Feedback;

namespace BEL.Api.Services;

public interface IFeedbackService
{
    Task<FeedbackResponse> CreateAsync(Guid userId, CreateFeedbackRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<FeedbackResponse>> ListAsync(CancellationToken cancellationToken);
}
