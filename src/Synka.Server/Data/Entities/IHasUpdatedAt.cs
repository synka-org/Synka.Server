namespace Synka.Server.Data.Entities;

/// <summary>
/// Marks an entity that tracks when it was last updated.
/// </summary>
public interface IHasUpdatedAt
{
    DateTimeOffset? UpdatedAt { get; set; }
}
