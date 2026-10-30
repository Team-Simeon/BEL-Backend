namespace BEL.Api.Contracts.Schools;

public sealed record SchoolResponse(
    Guid Id,
    string Name,
    string City,
    string? Address,
    bool IsActive,
    int TeacherCount,
    DateTime CreatedAtUtc);
