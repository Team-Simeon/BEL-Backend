using BEL.Api.Contracts.Users;
using BEL.Api.Data;
using BEL.Api.Exceptions;
using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Services;

public sealed class UserService(BelDbContext db) : IUserService
{
    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return user.ToResponse();
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(UserQuery query, CancellationToken cancellationToken)
    {
        var usersQuery = db.Users
            .AsNoTracking()
            .Include(u => u.School)
            .AsQueryable();

        if (query.Role is not null)
        {
            usersQuery = usersQuery.Where(u => u.Role == query.Role);
        }

        if (query.SchoolId is not null)
        {
            usersQuery = usersQuery.Where(u => u.SchoolId == query.SchoolId);
        }

        if (query.IsApproved is not null)
        {
            usersQuery = usersQuery.Where(u => u.IsApproved == query.IsApproved);
        }

        if (query.IsActive is not null)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            usersQuery = usersQuery.Where(u =>
                u.Email.Contains(term) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term));
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserResponse>(
            users.Select(u => u.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task<UserResponse> AdminUpdateAsync(
        Guid targetUserId,
        Guid adminUserId,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var role = request.Role
            ?? throw new BadRequestException("Role is required.");

        if (targetUserId == adminUserId &&
            (role != UserRole.Admin || !request.IsActive))
        {
            throw new BadRequestException(
                "Administrators cannot remove their own admin role or deactivate their own account.");
        }

        await EnsureAnotherActiveAdminRemainsAsync(user, role, request.IsActive, cancellationToken);

        School? school = null;
        if (role == UserRole.Teacher)
        {
            if (request.SchoolId is null || request.SchoolId == Guid.Empty)
            {
                throw new BadRequestException("A teacher must be assigned to a school.");
            }

            school = await db.Schools
                .FirstOrDefaultAsync(s => s.Id == request.SchoolId && s.IsActive, cancellationToken)
                ?? throw new BadRequestException("The selected school does not exist or is inactive.");
        }

        var isApproved = role == UserRole.Teacher ? request.IsApproved : true;
        var accessChanged = user.Role != role || user.IsActive != request.IsActive || user.IsApproved != isApproved;

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Role = role;
        user.SchoolId = school?.Id;
        user.School = school;
        user.IsApproved = isApproved;
        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (accessChanged)
        {
            // Sign the user out everywhere so old tokens never regain access, e.g. after reactivation.
            user.SecurityStamp = Guid.NewGuid();
        }

        await db.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task<UserResponse> ApproveTeacherAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.Role != UserRole.Teacher)
        {
            throw new BadRequestException("Only teacher accounts can be approved.");
        }

        if (!user.IsApproved)
        {
            user.IsApproved = true;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return user.ToResponse();
    }

    public async Task DeleteAsync(Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (targetUserId == currentUserId)
        {
            throw new BadRequestException("Administrators cannot delete their own account.");
        }

        await EnsureAnotherActiveAdminRemainsAsync(user, UserRole.Student, isActive: false, cancellationToken);

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAnotherActiveAdminRemainsAsync(
        User user,
        UserRole newRole,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (user.Role != UserRole.Admin || (newRole == UserRole.Admin && isActive))
        {
            return;
        }

        var anotherActiveAdminExists = await db.Users.AnyAsync(
            u => u.Id != user.Id && u.Role == UserRole.Admin && u.IsActive,
            cancellationToken);

        if (!anotherActiveAdminExists)
        {
            throw new BadRequestException("At least one active administrator must remain.");
        }
    }
}
