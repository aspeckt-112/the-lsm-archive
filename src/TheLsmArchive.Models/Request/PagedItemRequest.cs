using TheLsmArchive.Models.Request.Abstractions;

namespace TheLsmArchive.Models.Request;

/// <summary>
/// A paged item request.
/// </summary>
/// <param name="PageNumber">The page number. 1-based, default is 1.</param>
/// <param name="PageSize">The page size. Default is 50.</param>
/// <param name="SearchTerm">An optional search term to filter results.</param>
public record PagedItemRequest(
    int PageNumber = 1,
    int PageSize = 50,
    string? SearchTerm = null) : PagedRequest(PageNumber, PageSize);
