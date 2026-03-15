using TheLsmArchive.Models.Request.Abstractions;
using TheLsmArchive.Web.Api.Infrastructure;

namespace TheLsmArchive.Web.Api.Features.Search;

/// <summary>
/// The search service.
/// </summary>
public sealed class SearchService : ISearchService
{
    private record SearchProjection
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public EntityType Type { get; init; }
    }

    private readonly ILogger<SearchService> _logger;

    private readonly ReadOnlyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="readOnlyDbContext">The read-only database context.</param>
    public SearchService(
        ILogger<SearchService> logger,
        ReadOnlyDbContext readOnlyDbContext)
    {
        _logger = logger;
        _dbContext = readOnlyDbContext;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="searchRequest"/> is null.</exception>
    /// <exception cref="UnsupportedEntityTypeException">Thrown when an unsupported entity type is provided in the <paramref name="searchRequest"/>.</exception>
    public async Task<PagedResponse<SearchResult>> RunSearchAsync(
        SearchRequest searchRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(searchRequest);

        _logger.LogInformation("Running search with query: {Query}", searchRequest);

        string pattern = $"%{searchRequest.SearchTerm}%";

        return searchRequest.EntityType switch
        {
            EntityType.All => await SearchAll(pattern, searchRequest, cancellationToken),
            EntityType.Person => await SearchPeople(pattern, searchRequest, cancellationToken),
            EntityType.Topic => await SearchTopics(pattern, searchRequest, cancellationToken),
            EntityType.Episode => await SearchEpisodes(pattern, searchRequest, cancellationToken),
            _ => throw UnsupportedEntityTypeException.For(searchRequest.EntityType)
        };
    }

    private Task<PagedResponse<SearchResult>> SearchAll(
        string pattern,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<SearchProjection> union = QueryPersons(pattern)
            .Union(QueryTopics(pattern));

        if (request is SearchRequest { SearchTerm.Length: >= 5 })
        {
            union = union.Union(QueryEpisodes(pattern));
        }

        return BuildPagedResponse(union, request, cancellationToken);
    }

    private Task<PagedResponse<SearchResult>> SearchPeople(
        string pattern,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching people with pattern: {Pattern}", pattern);

        return BuildPagedResponse(QueryPersons(pattern), request, cancellationToken);
    }

    private Task<PagedResponse<SearchResult>> SearchTopics(string pattern, PagedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching topics with pattern: {Pattern}", pattern);

        return BuildPagedResponse(QueryTopics(pattern), request, cancellationToken);
    }

    private Task<PagedResponse<SearchResult>> SearchEpisodes(
        string pattern,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching episodes with pattern: {Pattern}", pattern);

        return BuildPagedResponse(QueryEpisodes(pattern), request, cancellationToken);
    }

    private async Task<PagedResponse<SearchResult>> BuildPagedResponse(
        IQueryable<SearchProjection> query,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResponse<SearchResult>([], 0, request.PageNumber, request.PageSize);
        }

        List<SearchResult> items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .WithPaging(request)
            .Select(x => new SearchResult(x.Id, x.Name, x.Type))
            .ToListAsync(cancellationToken);

        return new PagedResponse<SearchResult>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private IQueryable<SearchProjection> QueryPersons(string pattern)
    {
        return _dbContext.Persons
            .Where(p => EF.Functions.ILike(p.Name, pattern))
            .Select(p => new SearchProjection { Id = p.Id, Name = p.Name, Type = EntityType.Person });
    }

    private IQueryable<SearchProjection> QueryTopics(string pattern)
    {
        return _dbContext.Topics
            .Where(t => EF.Functions.ILike(t.Name, pattern))
            .Select(t => new SearchProjection { Id = t.Id, Name = t.Name, Type = EntityType.Topic });
    }

    private IQueryable<SearchProjection> QueryEpisodes(string pattern)
    {
        return _dbContext.Episodes
            .Where(e => EF.Functions.ILike(e.Title, pattern) ||
                        EF.Functions.ILike(e.PatreonPost.Summary, pattern))
            .Select(e => new SearchProjection { Id = e.Id, Name = e.Title, Type = EntityType.Episode });
    }
}
