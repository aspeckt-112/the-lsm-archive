namespace TheLsmArchive.Web.Api.Features.Search;

/// <summary>
/// The exception thrown when an unsupported search type is encountered.
/// </summary>
public class UnsupportedEntityTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedEntityTypeException"/> class.
    /// </summary>
    /// <param name="entityType"></param>
    private UnsupportedEntityTypeException(EntityType entityType)
        : base("Unsupported entity type: " + entityType)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="UnsupportedEntityTypeException"/> class for the specified entity type.
    /// </summary>
    /// <param name="entityType">The unsupported entity type.</param>
    /// <returns>A new instance of the <see cref="UnsupportedEntityTypeException"/> class.</returns>
    public static UnsupportedEntityTypeException For(EntityType entityType) => new(entityType);
}
