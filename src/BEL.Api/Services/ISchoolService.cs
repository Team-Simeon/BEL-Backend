using BEL.Api.Contracts.Schools;

namespace BEL.Api.Services;

public interface ISchoolService
{
    Task<IReadOnlyList<SchoolResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<SchoolResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SchoolResponse> CreateAsync(CreateSchoolRequest request, CancellationToken cancellationToken);

    Task<SchoolResponse> UpdateAsync(Guid id, UpdateSchoolRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
