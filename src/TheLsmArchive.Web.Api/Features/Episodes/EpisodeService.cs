using System.Linq.Expressions;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Web.Api.Infrastructure;

namespace TheLsmArchive.Web.Api.Features.Episodes;

/// <summary>
/// The episode service.
/// </summary>
public sealed class EpisodeService : IEpisodeService
{
    private readonly ILogger<EpisodeService> _logger;

    private readonly ReadOnlyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="readOnlyDbContext">The read-only database context.</param>
    public EpisodeService(
        ILogger<EpisodeService> logger,
        ReadOnlyDbContext readOnlyDbContext)
    {
        _logger = logger;
        _dbContext = readOnlyDbContext;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<Episode?> GetById(int id, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting episode with ID: {Id}", id);

        Expression<Func<EpisodeEntity, Episode>> mapToEpisode =
            episode => new Episode(
                Id: episode.Id,
                Title: episode.Title,
                ReleaseDate: DateOnly.FromDateTime(episode.ReleaseDateUtc.DateTime),
                PatreonPostLink: episode.PatreonPost.Link,
                SummaryHtml: episode.PatreonPost.Summary);

        return _dbContext.Episodes
            .Include(episode => episode.PatreonPost)
            .Where(e => e.Id == id)
            .Select(mapToEpisode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public async Task<PagedResponse<Episode>> GetByPersonId(
        int id,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting episodes for person with ID: {Id}", id);

        Expression<Func<PersonEpisodeEntity, Episode>> mapToEpisode =
            personEpisode => new Episode(
                Id: personEpisode.Episode.Id,
                Title: personEpisode.Episode.Title,
                ReleaseDate: DateOnly.FromDateTime(personEpisode.Episode.ReleaseDateUtc.UtcDateTime),
                PatreonPostLink: personEpisode.Episode.PatreonPost.Link,
                SummaryHtml: personEpisode.Episode.PatreonPost.Summary);

        IQueryable<PersonEpisodeEntity> baseQuery = _dbContext.PersonEpisodes
            .Include(pe => pe.Episode)
                .ThenInclude(e => e.PatreonPost)
            .Where(pe => pe.PersonId == id);

        if (!string.IsNullOrWhiteSpace(pagedRequest.SearchTerm))
        {
            baseQuery = baseQuery.Where(pe =>
                EF.Functions.ILike(pe.Episode.Title, $"%{pagedRequest.SearchTerm}%") ||
                EF.Functions.ILike(pe.Episode.PatreonPost.Summary, $"%{pagedRequest.SearchTerm}%"));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        List<Episode> items = await baseQuery
            .OrderBy(pe => pe.Episode.Title)
            .WithPaging(pagedRequest)
            .Select(mapToEpisode)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Episode>(items, totalCount, pagedRequest.PageNumber, pagedRequest.PageSize);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<List<Episode>> GetByTopicId(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting episodes for topic with ID: {Id}", id);

        Expression<Func<TopicEpisodeEntity, Episode>> mapToEpisode =
            topicEpisode => new Episode(
                Id: topicEpisode.Episode.Id,
                Title: topicEpisode.Episode.Title,
                ReleaseDate: DateOnly.FromDateTime(topicEpisode.Episode.ReleaseDateUtc.UtcDateTime),
                PatreonPostLink: topicEpisode.Episode.PatreonPost.Link,
                SummaryHtml: topicEpisode.Episode.PatreonPost.Summary);

        return _dbContext.TopicEpisodes
            .Include(te => te.Episode)
                .ThenInclude(e => e.PatreonPost)
            .Where(te => te.TopicId == id)
            .Select(mapToEpisode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Episode>> GetRecent(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting recent episodes from the last 7 days.");

        DateTimeOffset lastWeek = DateTimeOffset.UtcNow.AddDays(-7);

        Expression<Func<EpisodeEntity, Episode>> mapToEpisode =
            episode => new Episode(
                Id: episode.Id,
                Title: episode.Title,
                ReleaseDate: DateOnly.FromDateTime(episode.ReleaseDateUtc.UtcDateTime),
                PatreonPostLink: episode.PatreonPost.Link,
                SummaryHtml: episode.PatreonPost.Summary);

        return _dbContext.Episodes
            .Include(e => e.PatreonPost)
            .Where(e => e.ReleaseDateUtc >= lastWeek)
            .OrderByDescending(e => e.ReleaseDateUtc)
            .Select(mapToEpisode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetRandomEpisodeId(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting a random existing episode ID.");

        return _dbContext.Episodes
            .OrderBy(_ => EF.Functions.Random())
            .Select(episode => episode.Id)
            .FirstAsync(cancellationToken);
    }
}