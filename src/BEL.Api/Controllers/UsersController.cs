using BEL.Api.Contracts.Users;
using BEL.Api.Exceptions;
using BEL.Api.Security;
using BEL.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEL.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> List(
        [FromQuery] UserQuery query,
        CancellationToken cancellationToken)
    {
        var response = await userService.ListAsync(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var response = await userService.GetByIdAsync(User.GetUserId(), cancellationToken);
        return Ok(response);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateCurrent(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.UpdateProfileAsync(
            User.GetUserId(),
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        EnsureSelfOrAdmin(id);
        var response = await userService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<UserResponse>> AdminUpdate(
        Guid id,
        [FromBody] AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.AdminUpdateAsync(
            id,
            User.GetUserId(),
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<UserResponse>> ApproveTeacher(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await userService.ApproveTeacherAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    private void EnsureSelfOrAdmin(Guid requestedUserId)
    {
        if (requestedUserId != User.GetUserId() && !User.IsAdmin())
        {
            throw new ForbiddenException("You can only view your own profile.");
        }
    }
}
