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
    public async Task<TopicDetails?> GetDetailsById(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting details for topic with ID: {Id}", id);

        var result = await _dbContext.Topics
        .Include(t => t.TopicEpisodes)
        .ThenInclude(te => te.Episode)
        .Where(t => t.Id == id)
        .Select(t => new
        {
            FirstDiscussedUtc = t.TopicEpisodes
                    .Min(te => te.Episode.ReleaseDateUtc),
            LastDiscussedUtc = t.TopicEpisodes
                    .Max(te => te.Episode.ReleaseDateUtc)
        })
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? null
            : new TopicDetails(
                FirstDiscussed: DateOnly.FromDateTime(result.FirstDiscussedUtc.DateTime),
                LastDiscussed: DateOnly.FromDateTime(result.LastDiscussedUtc.DateTime));
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public async Task<PagedResponse<Episode>> GetEpisodesByTopicId(
        int id,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting episodes for topic with ID: {Id}", id);

        Expression<Func<TopicEpisodeEntity, Episode>> mapToEpisode =
            topicEpisode => new Episode(
                Id: topicEpisode.Episode.Id,
                Title: topicEpisode.Episode.Title,
                ReleaseDate: DateOnly.FromDateTime(topicEpisode.Episode.ReleaseDateUtc.DateTime),
                PatreonPostLink: topicEpisode.Episode.PatreonPost.Link,
                SummaryHtml: topicEpisode.Episode.PatreonPost.Summary);

        IQueryable<TopicEpisodeEntity> baseQuery = _dbContext.TopicEpisodes
            .Include(te => te.Episode)
                .ThenInclude(e => e.PatreonPost)
            .Where(te => te.TopicId == id);

        if (!string.IsNullOrWhiteSpace(pagedRequest.SearchTerm))
        {
            baseQuery = baseQuery.Where(te =>
                EF.Functions.ILike(te.Episode.Title, $"%{pagedRequest.SearchTerm}%") ||
                EF.Functions.ILike(te.Episode.PatreonPost.Summary, $"%{pagedRequest.SearchTerm}%"));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        List<Episode> items = await baseQuery
            .OrderBy(te => te.Episode.Title)
            .WithPaging(pagedRequest)
            .Select(mapToEpisode)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Episode>(items, totalCount, pagedRequest.PageNumber, pagedRequest.PageSize);
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
