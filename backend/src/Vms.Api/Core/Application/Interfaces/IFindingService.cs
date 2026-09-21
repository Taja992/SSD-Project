using System.Security.Claims;
using Vms.Api.Core.Domain.Dto.Finding;
using Vms.Domain.Enums;

namespace Vms.Api.Core.Application.Interfaces;

public interface IFindingService
{
    Task<PagedResult<FindingDto>> SearchAsync(
        FindingStatus? status, Severity? severity, string? assignee,
        int page, int pageSize, CancellationToken ct = default);

    Task<FindingDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<FindingDto> CreateAsync(CreateFindingDto dto, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Returns null if not found, throws ForbiddenException if not permitted.</summary>
    Task<FindingDto?> UpdateAsync(Guid id, UpdateFindingDto dto, ClaimsPrincipal user, CancellationToken ct = default);

    Task<FindingDto?> TriageAsync(Guid id, TriageFindingDto dto, CancellationToken ct = default);
}
