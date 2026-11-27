using BEL.Api.Contracts.Feedback;
using BEL.Api.Security;
using BEL.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEL.Api.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public sealed class FeedbackController(IFeedbackService feedbackService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FeedbackResponse>> Create(
        [FromBody] CreateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var response = await feedbackService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return Created($"/api/feedback/{response.Id}", response);
    }
}
