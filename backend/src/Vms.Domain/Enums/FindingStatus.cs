namespace Vms.Domain.Enums;

/// <summary>Lifecycle states from the exam spec §3.</summary>
public enum FindingStatus
{
    New = 1,          // Intake; not yet triaged
    Triaged = 2,      // Severity set, owner assigned
    InProgress = 3,   // Being remediated
    Remediated = 4,   // Fixed and verified
    AcceptedRisk = 5, // Deliberately not fixed — a decision, not a failure
}
