using BEL.Api.Models;

namespace BEL.Api.Contracts.Schools;

public static class SchoolMappings
{
    public static IQueryable<SchoolResponse> ToResponse(this IQueryable<School> schools) =>
        schools.Select(school => new SchoolResponse(
            school.Id,
            school.Name,
            school.City,
            school.Address,
            school.IsActive,
            school.Users.Count(user => user.Role == UserRole.Teacher),
            school.CreatedAtUtc));
}
