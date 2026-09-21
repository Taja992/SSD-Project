using Vms.Domain.Entities.Interfaces;
using Vms.Domain.Enums;

namespace Vms.Domain.Entities;

public class Finding : ITrackable, IAssignable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // --- Reporter-supplied. Treat as hostile: in Step 4 this arrives from
    // --- the unauthenticated public form (T-ER-02, T-ER-03).
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string AffectedAsset { get; set; }

    // --- Triage fields. Analyst/PM only — a Developer must never set these.
    public Severity Severity { get; set; } = Severity.Low;
    public FindingStatus Status { get; set; } = FindingStatus.New;
    public string? AssigneeUsername { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    // --- Server-controlled. Never bound from a request body.
    public FindingSource Source { get; set; } = FindingSource.Internal;
    public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByUsername { get; set; }

    /// <summary>External reporter contact (spec §3). PII — never in list responses.</summary>
    public string? ReporterEmail { get; set; }

    // ITrackable — maintained by the DbContext, not by callers.
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The one authorization question this entity can answer about itself.
    /// Pure, so it is unit-testable with no database and no HTTP (§10).
    /// </summary>
    public bool IsEditableBy(string username, bool isAnalystOrPm) =>
        isAnalystOrPm || AssigneeUsername == username;
}
