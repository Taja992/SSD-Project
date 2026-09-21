namespace Vms.Domain.Entities.Interfaces;

/// <summary>
/// Entities that can be assigned to a specific user. The analogue of Wishile's
/// IOwned — but note the difference: assignment here scopes WRITES only.
/// Per Q1 any staff member may read any finding.
/// </summary>
public interface IAssignable
{
    string? AssigneeUsername { get; set; }
}
