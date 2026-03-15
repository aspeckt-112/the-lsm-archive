namespace TheLsmArchive.Web.Api.Features.Search;

/// <summary>
/// The abstraction for a service to perform searches.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Runs a search based on the provided search request.
    /// </summary>
    /// <param name="searchRequest">The search request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged response containing search results.</returns>
    public Task<PagedResponse<SearchResult>> RunSearchAsync(
        SearchRequest searchRequest,
         CancellationToken cancellationToken);
}
