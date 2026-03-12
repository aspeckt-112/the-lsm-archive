namespace TheLsmArchive.Database.Entities.Abstractions;

/// <summary>
/// The base entity class.
/// </summary>
/// <remarks>
/// This class provides a common Id property for all entities.
/// </remarks>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public int Id { get; set; }
}
