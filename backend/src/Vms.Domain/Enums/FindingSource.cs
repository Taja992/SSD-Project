namespace Vms.Domain.Enums;

/// <summary>
/// The three intake channels. Source is a trust signal: External findings
/// arrive from an unauthenticated form and must never be rendered as HTML.
/// </summary>
public enum FindingSource
{
    Internal = 1,
    External = 2,
    Scanner = 3,
}
