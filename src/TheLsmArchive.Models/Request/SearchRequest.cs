using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request.Abstractions;

namespace TheLsmArchive.Models.Request;

/// <summary>
/// A request to search the archive.
/// </summary>
/// <param name="SearchTerm">The term to search for.</param>
/// <param name="EntityType">The type of entity to search for.</param>
/// <param name="PageNumber">The page number. 1-based, default is 1.</param>
/// <param name="PageSize">The page size. Default is 50.</param>
public record SearchRequest(
    string SearchTerm,
    EntityType EntityType = EntityType.All,
    int PageNumber = 1,
    int PageSize = 50) : PagedRequest(PageNumber, PageSize);
