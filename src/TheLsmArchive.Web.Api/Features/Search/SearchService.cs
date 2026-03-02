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
    public async Task<List<SearchResult>> RunSearchAsync(
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

    private Task<List<SearchResult>> SearchAll(
        string pattern,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<SearchProjection> union = QueryPersons(pattern)
            .Union(QueryTopics(pattern))
            .Union(QueryEpisodes(pattern));

        return union
            .OrderBy(x => x.Name)
            .WithPaging(request)
            .Select(x => new SearchResult(x.Id, x.Name, x.Type))
            .ToListAsync(cancellationToken);
    }

    private Task<List<SearchResult>> SearchPeople(
        string pattern,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching people with pattern: {Pattern}", pattern);

        return QueryPersons(pattern)
            .OrderBy(x => x.Name)
            .WithPaging(request)
            .Select(x => new SearchResult(x.Id, x.Name, x.Type))
            .ToListAsync(cancellationToken);
    }

    private Task<List<SearchResult>> SearchTopics(string pattern, PagedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching topics with pattern: {Pattern}", pattern);

        return QueryTopics(pattern)
            .OrderBy(x => x.Name)
            .WithPaging(request)
            .Select(x => new SearchResult(x.Id, x.Name, x.Type))
            .ToListAsync(cancellationToken);
    }

    private Task<List<SearchResult>> SearchEpisodes(string pattern, PagedRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching episodes with pattern: {Pattern}", pattern);

        return QueryEpisodes(pattern)
            .OrderBy(x => x.Name)
            .WithPaging(request)
            .Select(x => new SearchResult(x.Id, x.Name, x.Type))
            .ToListAsync(cancellationToken);
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
            .Include(e => e.PatreonPost)
            .Where(e => EF.Functions.ILike(e.Title, pattern) ||
                        EF.Functions.ILike(e.PatreonPost.Summary, pattern))
            .Select(e => new SearchProjection { Id = e.Id, Name = e.Title, Type = EntityType.Episode });
    }
}
