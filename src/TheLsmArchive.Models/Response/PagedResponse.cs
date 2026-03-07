namespace TheLsmArchive.Models.Response;

/// <summary>
/// A paged response containing items and pagination metadata.
/// </summary>
/// <param name="Items">The items for the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="PageNumber">The current page number (1-based).</param>
/// <param name="PageSize">The page size.</param>
/// <typeparam name="T">The type of items in the response.</typeparam>
public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
