namespace Synka.Server.Data.Entities;

/// <summary>
/// Marks an entity that tracks when it was created.
/// </summary>
public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}
