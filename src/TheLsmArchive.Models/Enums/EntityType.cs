namespace TheLsmArchive.Models.Enums;

/// <summary>
/// The type of search to perform.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// Search all types.
    /// </summary>
    All = 0,

    /// <summary>
    /// Search for persons.
    /// </summary>
    Person = 1,

    /// <summary>
    /// Search for topics.
    /// </summary>
    Topic = 2,

    /// <summary>
    /// Search for episodes.
    /// </summary>
    Episode = 3
}
