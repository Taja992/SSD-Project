using System.ComponentModel.DataAnnotations;

namespace Vms.Api.Core.Application.Validators;

/// <summary>
/// An SLA(Service Level Agreement) due date in the past is meaningless — it
/// would make a finding instantly overdue on the FR-RPT-04 dashboard.
/// Null is allowed: not every finding has a deadline yet.
/// </summary>
public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null) return ValidationResult.Success;   // optional field
        if (value is DateTimeOffset d && d <= DateTimeOffset.UtcNow)
            return new ValidationResult("Due date must be in the future.");
        return ValidationResult.Success;
    }
}
