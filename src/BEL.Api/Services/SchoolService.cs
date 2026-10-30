using BEL.Api.Contracts.Schools;
using BEL.Api.Data;
using BEL.Api.Exceptions;
using BEL.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BEL.Api.Services;

public sealed class SchoolService(BelDbContext db) : ISchoolService
{
    public async Task<IReadOnlyList<SchoolResponse>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var query = db.Schools.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(school => school.IsActive);
        }

        return await query
            .OrderBy(school => school.Name)
            .ToResponse()
            .ToListAsync(cancellationToken);
    }

    public async Task<SchoolResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var school = await db.Schools
            .AsNoTracking()
            .Where(s => s.Id == id)
            .ToResponse()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("School not found.");

        return school;
    }

    public async Task<SchoolResponse> CreateAsync(
        CreateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, excludedId: null, cancellationToken);

        var school = new School
        {
            Name = name,
            City = request.City.Trim(),
            Address = NormalizeAddress(request.Address),
            IsActive = true
        };

        db.Schools.Add(school);
        await db.SaveChangesAsync(cancellationToken);

        return new SchoolResponse(
            school.Id,
            school.Name,
            school.City,
            school.Address,
            school.IsActive,
            TeacherCount: 0,
            school.CreatedAtUtc);
    }

    public async Task<SchoolResponse> UpdateAsync(
        Guid id,
        UpdateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("School not found.");

        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, id, cancellationToken);

        school.Name = name;
        school.City = request.City.Trim();
        school.Address = NormalizeAddress(request.Address);
        school.IsActive = request.IsActive;
        school.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var school = await db.Schools.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("School not found.");

        var hasAssignedTeachers = await db.Users.AnyAsync(
            u => u.SchoolId == id,
            cancellationToken);

        if (hasAssignedTeachers)
        {
            throw new ConflictException(
                "The school cannot be deleted while teachers are assigned to it. Reassign them first.");
        }

        db.Schools.Remove(school);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameIsUniqueAsync(
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLower();
        var exists = await db.Schools.AnyAsync(
            s => s.Name.ToLower() == normalizedName &&
                 (!excludedId.HasValue || s.Id != excludedId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("A school with this name already exists.");
        }
    }

    private static string? NormalizeAddress(string? address) =>
        string.IsNullOrWhiteSpace(address) ? null : address.Trim();
}
