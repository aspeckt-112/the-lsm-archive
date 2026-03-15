namespace TheLsmArchive.Web.Api.Features.Search;

/// <summary>
/// The search endpoints.
/// </summary>
internal static class SearchEndpoints
{
    /// <summary>
    /// Adds the search endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        internal WebApplication AddSearchEndpoints()
        {
            RouteGroupBuilder search = app.MapGroup("/search").WithTags("Search");

            search.MapGet("/", Search)
                .WithName(nameof(Search))
                .WithSummary("Searches the archive.")
                .WithDescription("Performs a search across the archive based on the provided query parameters.")
                .Produces<Ok<PagedResponse<SearchResult>>>()
                .Produces<NoContent>()
                .Produces<BadRequest>();

            return app;
        }
    }

    private static async Task<Results<Ok<PagedResponse<SearchResult>>, NoContent>> Search(
        [AsParameters] SearchRequest searchRequest,
        [FromServices] ISearchService searchService,
        CancellationToken cancellationToken)
    {
        PagedResponse<SearchResult> result = await searchService.RunSearchAsync(searchRequest, cancellationToken);

        return result.TotalCount == 0
            ? TypedResults.NoContent()
            : TypedResults.Ok(result);
    }
}
