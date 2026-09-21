using Vms.Domain.Entities;
using Vms.Domain.Enums;

namespace Vms.Domain.Interfaces;

/// <summary>
/// Data access only. This layer NEVER decides who may do what — that is the
/// service's job. If you find yourself passing a ClaimsPrincipal in here, stop.
/// </summary>
public interface IFindingRepository
{
    Task<Finding?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Finding?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Finding> Items, int Total)> SearchAsync(
        FindingStatus? status, Severity? severity, string? assignee,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Finding finding, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
