using System.ComponentModel.DataAnnotations;
using Vms.Api.Core.Application.Validators;
using Vms.Domain.Enums;

namespace Vms.Api.Core.Domain.Dto.Finding;

/// <summary>
/// Analyst/PM create payload. No Id, no timestamps, no assignee.
/// Lengths MUST match the entity configuration (§5): the database caps are the
/// real boundary, these turn a driver-level 500 into a clean 400 and keep
/// oversized payloads away from the service layer (T-ER-06).
/// </summary>
public record CreateFindingDto(
    [property: Required, MaxLength(200)] string Title,
    [property: Required, MaxLength(10_000)] string Description,
    [property: Required, MaxLength(200)] string AffectedAsset,
    [property: EnumDataType(typeof(Severity))] Severity Severity);

/// <summary>
/// What ANY authorised editor may change. Deliberately has no Severity,
/// AssigneeUsername or DueDate — a Developer cannot reach those through this
/// endpoint at all, whatever they put in the JSON.
/// </summary>
public record UpdateFindingDto(
    [property: Required, MaxLength(200)] string Title,
    [property: Required, MaxLength(10_000)] string Description,
    [property: Required, MaxLength(200)] string AffectedAsset,
    [property: EnumDataType(typeof(FindingStatus))] FindingStatus Status);

/// <summary>Analyst/PM only — the triage fields (FIND-04).</summary>
public record TriageFindingDto(
    [property: EnumDataType(typeof(Severity))] Severity Severity,
    [property: MaxLength(100)] string? AssigneeUsername,
    [property: FutureDate] DateTimeOffset? DueDate);

/// <summary>
/// Outbound shape. ReporterEmail is absent by design — PII(Personally
/// Identifiable Information), and returning it would be T-API-09.
/// No validation attributes: responses are ours, not untrusted input.
/// </summary>
public record FindingDto(
    Guid Id,
    string Title,
    string Description,
    string AffectedAsset,
    string Severity,
    string Status,
    string? AssigneeUsername,
    DateTimeOffset? DueDate,
    string Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Paged envelope for FIND-05.</summary>
public record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);
