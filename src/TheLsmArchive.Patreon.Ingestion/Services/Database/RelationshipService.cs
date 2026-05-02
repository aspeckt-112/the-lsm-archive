using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

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
    /// Ensures each person in <paramref name="persons"/> is linked to <paramref name="episode"/>.
    /// Persons that are already linked are silently skipped.
    /// </summary>
    /// <param name="persons">The persons to link.</param>
    /// <param name="episode">The episode to link them to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkPersonsToEpisodeAsync(
        IReadOnlyCollection<PersonEntity> persons,
        EpisodeEntity episode,
        CancellationToken cancellationToken)
    {
        if (persons.Count == 0)
        {
            return;
        }

        HashSet<int> existingPersonIds = await _dbContext.PersonEpisodes
            .Where(pe => pe.EpisodeId == episode.Id)
            .Select(pe => pe.PersonId)
            .ToHashSetAsync(cancellationToken);

        foreach (PersonEntity person in persons)
        {
            if (existingPersonIds.Contains(person.Id))
            {
                continue;
            }

            _dbContext.PersonEpisodes.Add(new PersonEpisodeEntity
            {
                PersonId = person.Id,
                EpisodeId = episode.Id
            });
        }
    }

    /// <summary>
    /// Ensures each topic in <paramref name="topics"/> is linked to <paramref name="episode"/>.
    /// Topics that are already linked are silently skipped.
    /// </summary>
    /// <param name="topics">The topics to link.</param>
    /// <param name="episode">The episode to link them to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkTopicsToEpisodeAsync(
        IReadOnlyCollection<TopicEntity> topics,
        EpisodeEntity episode,
        CancellationToken cancellationToken)
    {
        if (topics.Count == 0)
        {
            return;
        }

        HashSet<int> existingTopicIds = await _dbContext.TopicEpisodes
            .Where(te => te.EpisodeId == episode.Id)
            .Select(te => te.TopicId)
            .ToHashSetAsync(cancellationToken);

        foreach (TopicEntity topic in topics)
        {
            if (existingTopicIds.Contains(topic.Id))
            {
                continue;
            }

            _dbContext.TopicEpisodes.Add(new TopicEpisodeEntity
            {
                TopicId = topic.Id,
                EpisodeId = episode.Id
            });
        }
    }

    /// <summary>
    /// Ensures every person–topic pair in the cross-product of <paramref name="persons"/> × <paramref name="topics"/> is linked.
    /// Pairs that are already linked are silently skipped.
    /// </summary>
    /// <param name="persons">The persons.</param>
    /// <param name="topics">The topics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkPersonsToTopicsAsync(
        IReadOnlyCollection<PersonEntity> persons,
        IReadOnlyCollection<TopicEntity> topics,
        CancellationToken cancellationToken)
    {
        if (persons.Count == 0 || topics.Count == 0)
        {
            return;
        }

        var personIds = persons.Select(p => p.Id).ToList();
        var topicIds = topics.Select(t => t.Id).ToList();

        var existingLinkRows = await _dbContext.PersonTopics
            .Where(pt => personIds.Contains(pt.PersonId) && topicIds.Contains(pt.TopicId))
            .Select(pt => new { pt.PersonId, pt.TopicId })
            .ToListAsync(cancellationToken);

        var existingLinks = existingLinkRows.Select(x => (x.PersonId, x.TopicId)).ToHashSet();

        foreach (PersonEntity person in persons)
        {
            foreach (TopicEntity topic in topics)
            {
                if (existingLinks.Contains((person.Id, topic.Id)))
                {
                    continue;
                }

                _dbContext.PersonTopics.Add(new PersonTopicEntity
                {
                    PersonId = person.Id,
                    TopicId = topic.Id
                });
            }
        }
    }
}
