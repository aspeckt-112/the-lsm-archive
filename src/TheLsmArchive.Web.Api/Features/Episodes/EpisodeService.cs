using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Web.Api.Infrastructure;

namespace TheLsmArchive.Web.Api.Features.Episodes;

/// <summary>
/// The episode service.
/// </summary>
public sealed class EpisodeService : IEpisodeService
{
    private readonly ILogger<EpisodeService> _logger;

    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public EpisodeService(
        ILogger<EpisodeService> logger,
        LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
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
            .AsNoTracking()
            .Include(episode => episode.PatreonPost)
            .Where(e => e.Id == id)
            .Select(mapToEpisode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public async Task<PagedResponse<PersonTimelineEntry>> GetByPersonId(
        int id,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting episodes for person with ID: {Id}", id);

        IQueryable<PersonEpisodeEntity> baseQuery = _dbContext.PersonEpisodes
            .AsNoTracking()
            .Include(pe => pe.Episode)
                .ThenInclude(e => e.PatreonPost)
            .Include(pe => pe.Episode)
                .ThenInclude(e => e.TopicEpisodes)
                    .ThenInclude(te => te.Topic)
            .Where(pe => pe.PersonId == id);

        if (!string.IsNullOrWhiteSpace(pagedRequest.SearchTerm))
        {
            baseQuery = baseQuery.Where(pe =>
                EF.Functions.ILike(pe.Episode.Title, $"%{pagedRequest.SearchTerm}%") ||
                pe.Episode.TopicEpisodes.Any(te => EF.Functions.ILike(te.Topic.Name, $"%{pagedRequest.SearchTerm}%")));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        IOrderedQueryable<PersonEpisodeEntity> orderedQuery = sortDescending
            ? baseQuery.OrderByDescending(pe => pe.Episode.ReleaseDateUtc)
            : baseQuery.OrderBy(pe => pe.Episode.ReleaseDateUtc);

        List<PersonTimelineEntry> items = await orderedQuery
            .WithPaging(pagedRequest)
            .Select(pe => new PersonTimelineEntry(
                EpisodeId: pe.Episode.Id,
                Title: pe.Episode.Title,
                ReleaseDate: DateOnly.FromDateTime(pe.Episode.ReleaseDateUtc.DateTime),
                PatreonPostLink: pe.Episode.PatreonPost.Link,
                Topics: pe.Episode.TopicEpisodes
                    .Select(te => new Topic(te.Topic.Id, te.Topic.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResponse<PersonTimelineEntry>(items, totalCount, pagedRequest.PageNumber, pagedRequest.PageSize);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<Episode?> GetMostRecentByPersonId(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting most recent episode for person with ID: {Id}", id);

        Expression<Func<PersonEpisodeEntity, Episode>> mapToEpisode =
            personEpisode => new Episode(
                Id: personEpisode.Episode.Id,
                Title: personEpisode.Episode.Title,
                ReleaseDate: DateOnly.FromDateTime(personEpisode.Episode.ReleaseDateUtc.UtcDateTime),
                PatreonPostLink: personEpisode.Episode.PatreonPost.Link,
                SummaryHtml: personEpisode.Episode.PatreonPost.Summary);

        return _dbContext.PersonEpisodes
            .AsNoTracking()
            .Include(pe => pe.Episode)
                .ThenInclude(e => e.PatreonPost)
            .Where(pe => pe.PersonId == id)
            .OrderByDescending(pe => pe.Episode.ReleaseDateUtc)
            .Select(mapToEpisode)
            .FirstOrDefaultAsync(cancellationToken);
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
            .AsNoTracking()
            .Include(e => e.PatreonPost)
            .Where(e => e.ReleaseDateUtc >= lastWeek)
            .OrderByDescending(e => e.ReleaseDateUtc)
            .Select(mapToEpisode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetRandomEpisodeId(
        CancellationToken cancellationToken)
    {
        // Make the assumption that at least one episode will exist in the database.

        _logger.LogInformation("Getting a random existing episode ID.");

        var bounds = await _dbContext.Episodes
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                MinId = group.Min(episode => episode.Id),
                MaxId = group.Max(episode => episode.Id),
            })
            .SingleAsync(cancellationToken);

        long randomCandidateId = Random.Shared.Next(bounds.MinId, bounds.MaxId + 1);

        return await _dbContext.Episodes
        .AsNoTracking()
        .Where(x => x.Id >= randomCandidateId)
        .Select(x => x.Id)
        .FirstAsync(cancellationToken);
    }
}
