using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for managing episode relationship records (person↔episode, topic↔episode, person↔topic).
/// </summary>
/// <remarks>
/// None of the methods in this service call <c>SaveChangesAsync</c>.
/// The caller is responsible for flushing changes, typically as part of a wider transaction.
/// All methods are idempotent: existing links are silently skipped, making them safe to call on retry.
/// </remarks>
public sealed class RelationshipService
{
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelationshipService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public RelationshipService(LsmArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Ensures each person in <paramref name="personIds"/> is linked to the episode with <paramref name="episodeId"/>.
    /// Persons that are already linked are silently skipped.
    /// </summary>
    /// <param name="personIds">The IDs of the persons to link.</param>
    /// <param name="episodeId">The ID of the episode to link them to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkPersonsToEpisodeAsync(
        IReadOnlyCollection<int> personIds,
        int episodeId,
        CancellationToken cancellationToken)
    {
        if (personIds.Count == 0)
        {
            return;
        }

        HashSet<int> existingPersonIds = await _dbContext.PersonEpisodes
            .Where(pe => pe.EpisodeId == episodeId)
            .Select(pe => pe.PersonId)
            .ToHashSetAsync(cancellationToken);

        foreach (int personId in personIds)
        {
            if (existingPersonIds.Contains(personId))
            {
                continue;
            }

            _dbContext.PersonEpisodes.Add(new PersonEpisodeEntity
            {
                PersonId = personId,
                EpisodeId = episodeId
            });
        }
    }

    /// <summary>
    /// Ensures each topic in <paramref name="topicIds"/> is linked to the episode with <paramref name="episodeId"/>.
    /// Topics that are already linked are silently skipped.
    /// </summary>
    /// <param name="topicIds">The IDs of the topics to link.</param>
    /// <param name="episodeId">The ID of the episode to link them to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkTopicsToEpisodeAsync(
        IReadOnlyCollection<int> topicIds,
        int episodeId,
        CancellationToken cancellationToken)
    {
        if (topicIds.Count == 0)
        {
            return;
        }

        HashSet<int> existingTopicIds = await _dbContext.TopicEpisodes
            .Where(te => te.EpisodeId == episodeId)
            .Select(te => te.TopicId)
            .ToHashSetAsync(cancellationToken);

        foreach (int topicId in topicIds)
        {
            if (existingTopicIds.Contains(topicId))
            {
                continue;
            }

            _dbContext.TopicEpisodes.Add(new TopicEpisodeEntity
            {
                TopicId = topicId,
                EpisodeId = episodeId
            });
        }
    }

    /// <summary>
    /// Ensures every person–topic pair in the cross-product of <paramref name="personIds"/> × <paramref name="topicIds"/> is linked.
    /// Pairs that are already linked are silently skipped.
    /// </summary>
    /// <param name="personIds">The IDs of the persons.</param>
    /// <param name="topicIds">The IDs of the topics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkPersonsToTopicsAsync(
        IReadOnlyCollection<int> personIds,
        IReadOnlyCollection<int> topicIds,
        CancellationToken cancellationToken)
    {
        if (personIds.Count == 0 || topicIds.Count == 0)
        {
            return;
        }

        var existingLinkRows = await _dbContext.PersonTopics
            .Where(pt => personIds.Contains(pt.PersonId) && topicIds.Contains(pt.TopicId))
            .Select(pt => new { pt.PersonId, pt.TopicId })
            .ToListAsync(cancellationToken);

        var existingLinks = existingLinkRows.Select(x => (x.PersonId, x.TopicId)).ToHashSet();

        foreach (int personId in personIds)
        {
            foreach (int topicId in topicIds)
            {
                if (existingLinks.Contains((personId, topicId)))
                {
                    continue;
                }

                _dbContext.PersonTopics.Add(new PersonTopicEntity
                {
                    PersonId = personId,
                    TopicId = topicId
                });
            }
        }
    }
}
