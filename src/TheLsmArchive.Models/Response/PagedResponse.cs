namespace TheLsmArchive.Models.Response;

/// <summary>
/// A paged response containing items and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public record PagedResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResponse{T}"/> record.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The page size. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize"/> is zero or negative.</exception>
    public PagedResponse(
        List<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Gets the items for the current page.
    /// </summary>
    public List<T> Items { get; init; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
