using Microsoft.EntityFrameworkCore;
using Vms.Domain.Entities;
using Vms.Domain.Enums;
using Vms.Domain.Interfaces;
using Vms.Infrastructure.Context;

namespace Vms.Infrastructure.Repositories;

public class FindingRepository(VmsDbContext db) : IFindingRepository
{
    /// <summary>Read-only fetch. AsNoTracking: this instance is never saved.</summary>
    public Task<Finding?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Findings.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    /// <summary>Tracked fetch for a write path.</summary>
    public Task<Finding?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default) =>
        db.Findings.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<(IReadOnlyList<Finding> Items, int Total)> SearchAsync(
        FindingStatus? status, Severity? severity, string? assignee, string? asset,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Findings.AsNoTracking().AsQueryable();

        // Filters are applied IN the query — never as a post-filter over a full
        // result set, which would read rows the caller may not be entitled to
        // and leak existence through timing (T-API-08).
        if (status is not null)   q = q.Where(f => f.Status == status);
        if (severity is not null) q = q.Where(f => f.Severity == severity);
        if (assignee is not null) q = q.Where(f => f.AssigneeUsername == assignee);
        // FIND-05: "search by affected asset" - a case-insensitive CONTAINS.
        // The user text becomes part of a LIKE pattern, so %, _ and \ are
        // escaped first: searching for "%" must find a literal %, not every row.
        if (!string.IsNullOrWhiteSpace(asset))
            q = q.Where(f => EF.Functions.ILike(f.AffectedAsset, $"%{EscapeLike(asset)}%", @"\"));


        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");


    public async Task AddAsync(Finding finding, CancellationToken ct = default) =>
        await db.Findings.AddAsync(finding, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
