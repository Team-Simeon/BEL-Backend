using BEL.Api.Contracts.Auth;
using BEL.Api.Contracts.Users;
using BEL.Api.Data;
using BEL.Api.Exceptions;
using BEL.Api.Models;
using BEL.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Services;

public sealed class AuthService(
    BelDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<RegistrationResponse> RegisterStudentAsync(
        RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        var email = await EnsureEmailIsAvailableAsync(request.Email, cancellationToken);

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = UserRole.Student,
            IsApproved = true,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateAccessToken(user);
        return new RegistrationResponse(user.ToResponse(), token.Token, token.ExpiresAtUtc);
    }

    public async Task<RegistrationResponse> RegisterTeacherAsync(
        RegisterTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var email = await EnsureEmailIsAvailableAsync(request.Email, cancellationToken);

        if (request.SchoolId is null || request.SchoolId == Guid.Empty)
        {
            throw new BadRequestException("A school must be selected for teacher registration.");
        }

        var school = await db.Schools
            .FirstOrDefaultAsync(s => s.Id == request.SchoolId && s.IsActive, cancellationToken);

        if (school is null)
        {
            throw new BadRequestException("The selected school does not exist or is inactive.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = UserRole.Teacher,
            SchoolId = school.Id,
            School = school,
            IsApproved = false,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        // Teachers receive a token only after an administrator approves the account.
        return new RegistrationResponse(user.ToResponse(), null, null);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account is deactivated.");
        }

        if (user.Role == UserRole.Teacher && !user.IsApproved)
        {
            throw new ForbiddenException("Teacher accounts must be approved by an administrator before signing in.");
        }

        var token = tokenService.CreateAccessToken(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, user.ToResponse());
    }

    public async Task LogoutAsync(
        Guid userId,
        string tokenId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Revoked entries are only needed until the token would have expired anyway.
        await db.RevokedTokens
            .Where(t => t.ExpiresAtUtc < now)
            .ExecuteDeleteAsync(cancellationToken);

        if (await db.RevokedTokens.AnyAsync(t => t.TokenId == tokenId, cancellationToken))
        {
            return;
        }

        db.RevokedTokens.Add(new RevokedToken
        {
            TokenId = tokenId,
            UserId = userId,
            ExpiresAtUtc = expiresAtUtc,
            RevokedAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutEverywhereAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.SecurityStamp = Guid.NewGuid();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> EnsureEmailIsAvailableAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);

        var exists = await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        return normalizedEmail;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
