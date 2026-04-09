using System.Linq.Expressions;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Web.Api.Infrastructure;

namespace TheLsmArchive.Web.Api.Features.Topics;

/// <summary>
/// The topic service.
/// </summary>
public sealed class TopicService : ITopicService
{
    private readonly ILogger<TopicService> _logger;
    private readonly ReadOnlyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="readOnlyDbContext">The read-only database context.</param>
    public TopicService(
        ILogger<TopicService> logger,
        ReadOnlyDbContext readOnlyDbContext)
    {
        _logger = logger;
        _dbContext = readOnlyDbContext;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<Topic?> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting topic with ID: {Id}", id);

        Expression<Func<TopicEntity, Topic>> mapToTopic =
            topic => new Topic(
                Id: topic.Id,
                Name: topic.Name);

        return _dbContext.Topics
            .Where(t => t.Id == id)
            .Select(mapToTopic)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public async Task<TopicTimeline?> GetTimeline(
        int id,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting timeline for topic with ID: {Id}", id);

        bool topicExists = await _dbContext.Topics
            .AnyAsync(t => t.Id == id, cancellationToken);

        if (!topicExists)
        {
            return null;
        }

        var dateRange = await _dbContext.TopicEpisodes
            .Where(te => te.TopicId == id)
            .GroupBy(te => te.TopicId)
            .Select(g => new
            {
                First = g.Min(te => te.Episode.ReleaseDateUtc),
                Last = g.Max(te => te.Episode.ReleaseDateUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        IQueryable<TopicEpisodeEntity> baseQuery = _dbContext.TopicEpisodes
            .Include(te => te.Episode)
                .ThenInclude(e => e.PatreonPost)
            .Include(te => te.Episode)
                .ThenInclude(e => e.PersonEpisodes)
                    .ThenInclude(pe => pe.Person)
            .Where(te => te.TopicId == id);

        if (!string.IsNullOrWhiteSpace(pagedRequest.SearchTerm))
        {
            baseQuery = baseQuery.Where(te =>
                EF.Functions.ILike(te.Episode.Title, $"%{pagedRequest.SearchTerm}%") ||
                te.Episode.PersonEpisodes.Any(pe => EF.Functions.ILike(pe.Person.Name, $"%{pagedRequest.SearchTerm}%")));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        IOrderedQueryable<TopicEpisodeEntity> orderedQuery = sortDescending
            ? baseQuery.OrderByDescending(te => te.Episode.ReleaseDateUtc)
            : baseQuery.OrderBy(te => te.Episode.ReleaseDateUtc);

        List<TopicTimelineEntry> items = await orderedQuery
            .WithPaging(pagedRequest)
            .Select(te => new TopicTimelineEntry(
                EpisodeId: te.Episode.Id,
                Title: te.Episode.Title,
                ReleaseDate: DateOnly.FromDateTime(te.Episode.ReleaseDateUtc.DateTime),
                PatreonPostLink: te.Episode.PatreonPost.Link,
                People: te.Episode.PersonEpisodes
                    .Select(pe => new Person(pe.Person.Id, pe.Person.Name))
                    .ToList()))
            .ToListAsync(cancellationToken);

        DateOnly firstDiscussed = dateRange is not null
            ? DateOnly.FromDateTime(dateRange.First.DateTime)
            : default;
        DateOnly lastDiscussed = dateRange is not null
            ? DateOnly.FromDateTime(dateRange.Last.DateTime)
            : default;

        return new TopicTimeline(
            firstDiscussed,
            lastDiscussed,
            new PagedResponse<TopicTimelineEntry>(items, totalCount, pagedRequest.PageNumber, pagedRequest.PageSize));
    }

    /// <summary>
    /// Gets topics by episode ID.
    /// </summary>
    /// <param name="id">The episode ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of topics associated with the episode.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<List<Topic>> GetByEpisodeId(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting topics for episode with ID: {Id}", id);

        Expression<Func<TopicEpisodeEntity, Topic>> mapToTopic =
            topicEpisode => new Topic(
                Id: topicEpisode.Topic.Id,
                Name: topicEpisode.Topic.Name);

        return _dbContext.TopicEpisodes
            .Include(te => te.Topic)
            .Where(te => te.EpisodeId == id)
            .Select(mapToTopic)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<List<MostDiscussedTopic>> GetMostDiscussedByPersonId(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting most discussed topics for person with ID: {Id}", id);

        const int mostDiscussedTopicCount = 25;

        return _dbContext.PersonEpisodes
            .Where(personEpisode => personEpisode.PersonId == id)
            .Join(
                _dbContext.TopicEpisodes,
                personEpisode => personEpisode.EpisodeId,
                topicEpisode => topicEpisode.EpisodeId,
                (_, topicEpisode) => new
                {
                    topicEpisode.TopicId,
                    topicEpisode.Topic.Name
                })
            .GroupBy(topic => new { topic.TopicId, topic.Name })
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.Name)
            .Take(mostDiscussedTopicCount)
            .Select(group => new MostDiscussedTopic(
                group.Key.TopicId,
                group.Key.Name,
                group.Count()))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<List<MostDiscussedTopic>> GetMostDiscussedAlongsideByTopicId(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting most discussed alongside topics for topic with ID: {Id}", id);

        const int mostDiscussedAlongsideCount = 25;

        return _dbContext.TopicEpisodes
            .Where(te => te.TopicId == id)
            .Join(
                _dbContext.TopicEpisodes,
                source => source.EpisodeId,
                other => other.EpisodeId,
                (_, other) => new
                {
                    other.TopicId,
                    other.Topic.Name
                })
            .Where(t => t.TopicId != id)
            .GroupBy(t => new { t.TopicId, t.Name })
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.Name)
            .Take(mostDiscussedAlongsideCount)
            .Select(group => new MostDiscussedTopic(
                group.Key.TopicId,
                group.Key.Name,
                group.Count()))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public async Task<PagedResponse<Topic>> GetByPersonId(
        int id,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting topics for person with ID: {Id}", id);

        Expression<Func<PersonTopicEntity, Topic>> mapToTopic =
            personTopic => new Topic(
                Id: personTopic.Topic.Id,
                Name: personTopic.Topic.Name);

        IQueryable<PersonTopicEntity> baseQuery = _dbContext.PersonTopics
            .Include(pt => pt.Topic)
            .Where(pt => pt.PersonId == id);

        if (!string.IsNullOrWhiteSpace(pagedRequest.SearchTerm))
        {
            baseQuery = baseQuery.Where(pt => EF.Functions.ILike(pt.Topic.Name, $"%{pagedRequest.SearchTerm}%"));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        List<Topic> items = await baseQuery
            .OrderBy(pt => pt.Topic.Name)
            .WithPaging(pagedRequest)
            .Select(mapToTopic)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Topic>(items, totalCount, pagedRequest.PageNumber, pagedRequest.PageSize);
    }
}
