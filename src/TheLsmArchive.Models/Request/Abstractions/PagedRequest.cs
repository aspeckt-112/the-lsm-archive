namespace TheLsmArchive.Models.Request.Abstractions;

/// <summary>
/// A paged request.
/// </summary>
/// <param name="PageNumber">The page number. 1-based.</param>
/// <param name="PageSize">The page size.</param>
public abstract record PagedRequest(int PageNumber, int PageSize);
