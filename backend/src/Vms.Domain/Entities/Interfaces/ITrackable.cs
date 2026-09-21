namespace Vms.Domain.Entities.Interfaces;

/// <summary>Entities whose timestamps are maintained by the DbContext, never by callers.</summary>
public interface ITrackable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
