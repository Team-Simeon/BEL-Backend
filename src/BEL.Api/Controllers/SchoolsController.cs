using BEL.Api.Contracts.Schools;
using BEL.Api.Exceptions;
using BEL.Api.Security;
using BEL.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEL.Api.Controllers;

[ApiController]
[Route("api/schools")]
[Authorize]
public sealed class SchoolsController(ISchoolService schoolService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SchoolResponse>>> List(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        if (includeInactive && !User.IsAdmin())
        {
            throw new ForbiddenException("Only administrators can view inactive schools.");
        }

        var response = await schoolService.ListAsync(includeInactive, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SchoolResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await schoolService.GetByIdAsync(id, cancellationToken);

        if (!response.IsActive && !User.IsAdmin())
        {
            throw new NotFoundException("School not found.");
        }

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<SchoolResponse>> Create(
        [FromBody] CreateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var response = await schoolService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<ActionResult<SchoolResponse>> Update(
        Guid id,
        [FromBody] UpdateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var response = await schoolService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await schoolService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
