using BEL.Api.Contracts.Auth;
using BEL.Api.Contracts.Users;
using BEL.Api.Security;
using BEL.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEL.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{
    [HttpPost("register/student")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResponse>> RegisterStudent(
        [FromBody] RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterStudentAsync(request, cancellationToken);
        return Created($"/api/users/{response.User.Id}", response);
    }

    [HttpPost("register/teacher")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResponse>> RegisterTeacher(
        [FromBody] RegisterTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterTeacherAsync(request, cancellationToken);
        return Created($"/api/users/{response.User.Id}", response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var response = await userService.GetByIdAsync(User.GetUserId(), cancellationToken);
        return Ok(response);
    }
}
