using System.Security.Claims;
using AutoMapper;
using Vms.Api.Core.Application.Exceptions;
using Vms.Api.Core.Application.Interfaces;
using Vms.Api.Core.Domain.Dto.Finding;
using Vms.Domain.Entities;
using Vms.Domain.Enums;
using Vms.Domain.Interfaces;

namespace Vms.Api.Core.Application.Services;

public class FindingService(IFindingRepository repo, IMapper mapper) : IFindingService
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<FindingDto>> SearchAsync(
        FindingStatus? status, Severity? severity, string? assignee, string? asset,
        int page, int pageSize, CancellationToken ct = default)
    {
        // T-API-10: an unbounded list endpoint is a denial-of-service primitive.
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        page = Math.Max(page, 1);

        var (items, total) = await repo.SearchAsync(status, severity, assignee, asset, page, pageSize, ct);
        return new PagedResult<FindingDto>(
            total, page, pageSize,
            items.Select(mapper.Map<FindingDto>).ToList());
    }

    public async Task<FindingDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        // Q1: any authenticated staff member may read any finding. The endpoint
        // group's RequireAuthorization("AnyStaff") already proved they are staff.
        var f = await repo.GetByIdAsync(id, ct);
        return f is null ? null : mapper.Map<FindingDto>(f);
    }

    public async Task<FindingDto> CreateAsync(
        CreateFindingDto dto, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var finding = mapper.Map<Finding>(dto);

        // Server-controlled, set AFTER mapping so no request body can reach them.
        finding.Status = FindingStatus.New;
        finding.Source = FindingSource.Internal;
        finding.CreatedByUsername = user.FindFirstValue("preferred_username");
        finding.DiscoveredAt = DateTimeOffset.UtcNow;

        await repo.AddAsync(finding, ct);
        await repo.SaveChangesAsync(ct);
        return mapper.Map<FindingDto>(finding);
    }

    public async Task<FindingDto?> UpdateAsync(
        Guid id, UpdateFindingDto dto, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var finding = await repo.GetByIdForUpdateAsync(id, ct);
        if (finding is null) return null;

        // ---- T-API-03. THE check. Without it any Developer edits any finding.
        var username = user.FindFirstValue("preferred_username");
        var isAnalystOrPm = user.IsInRole("Analyst") || user.IsInRole("PM");

        if (username is null || !finding.IsEditableBy(username, isAnalystOrPm))
            throw new ForbiddenException("Not permitted to edit this finding.");
        // ----------------------------------------------------------------

        // Assigned by hand, not by AutoMapper. Four fields, visibly four.
        finding.Title = dto.Title;
        finding.Description = dto.Description;
        finding.AffectedAsset = dto.AffectedAsset;
        finding.Status = dto.Status;

        await repo.SaveChangesAsync(ct);
        return mapper.Map<FindingDto>(finding);
    }

    public async Task<FindingDto?> TriageAsync(
        Guid id, TriageFindingDto dto, CancellationToken ct = default)
    {
        // Role gate is on the endpoint (AnalystOrPm). Triage is not
        // assignee-scoped: an Analyst triages findings nobody owns yet.
        var finding = await repo.GetByIdForUpdateAsync(id, ct);
        if (finding is null) return null;

        finding.Severity = dto.Severity;
        finding.AssigneeUsername = dto.AssigneeUsername;
        finding.DueDate = dto.DueDate;
        if (finding.Status == FindingStatus.New) finding.Status = FindingStatus.Triaged;

        await repo.SaveChangesAsync(ct);
        return mapper.Map<FindingDto>(finding);
    }
}
