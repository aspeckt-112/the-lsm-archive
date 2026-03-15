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
    /// <remarks>
    /// When <see cref="EntityType.All"/> is specified, episode results are only included when
    /// <see cref="SearchRequest.SearchTerm"/> is at least 5 characters long. Shorter terms will
    /// still match people and topics. Use <see cref="EntityType.Episode"/> directly to search
    /// episodes regardless of term length.
    /// </remarks>
    public Task<PagedResponse<SearchResult>> RunSearchAsync(
        SearchRequest searchRequest,
         CancellationToken cancellationToken);
}
