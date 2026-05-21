using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

internal static class EpisodeTestDataHelper
{
    internal static async Task<EpisodeEntity> CreateEpisodeAsync(
        LsmArchiveDbContext dbContext,
        ShowEntity show,
        string title,
        DateTimeOffset releaseDateUtc,
        int patreonId,
        CancellationToken cancellationToken,
        string? summaryHtml = null,
        string? patreonPostLink = null)
    {
        PatreonPostEntity patreonPost = new()
        {
            ShowId = show.Id,
            PatreonId = patreonId,
            Title = title,
            Published = releaseDateUtc,
            Summary = summaryHtml ?? $"<p>Summary for {title}</p>",
            Link = patreonPostLink ?? $"https://www.patreon.com/posts/{patreonId}",
            AudioUrl = $"https://www.patreon.com/posts/{patreonId}/audio"
        };

        await dbContext.PatreonPosts.AddAsync(patreonPost, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        EpisodeEntity episode = new()
        {
            ShowId = show.Id,
            Title = title,
            ReleaseDateUtc = releaseDateUtc,
            PatreonPostId = patreonPost.Id
        };

        await dbContext.Episodes.AddAsync(episode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        patreonPost.EpisodeId = episode.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return episode;
    }

    internal static async Task<PersonEntity> CreatePersonAsync(
        LsmArchiveDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        PersonEntity person = new()
        {
            Name = name,
            NormalizedName = name.ToLowerInvariant()
        };

        await dbContext.Persons.AddAsync(person, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return person;
    }

    internal static async Task<TopicEntity> CreateTopicAsync(
        LsmArchiveDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        TopicEntity topic = new()
        {
            Name = name,
            NormalizedName = name.ToLowerInvariant()
        };

        await dbContext.Topics.AddAsync(topic, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return topic;
    }

    internal static async Task<PersonEpisodeEntity> LinkPersonToEpisodeAsync(
        LsmArchiveDbContext dbContext,
        PersonEntity person,
        EpisodeEntity episode,
        CancellationToken cancellationToken)
    {
        PersonEpisodeEntity personEpisode = new()
        {
            PersonId = person.Id,
            EpisodeId = episode.Id
        };

        await dbContext.PersonEpisodes.AddAsync(personEpisode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return personEpisode;
    }

    internal static async Task<TopicEpisodeEntity> LinkTopicToEpisodeAsync(
        LsmArchiveDbContext dbContext,
        TopicEntity topic,
        EpisodeEntity episode,
        CancellationToken cancellationToken)
    {
        TopicEpisodeEntity topicEpisode = new()
        {
            TopicId = topic.Id,
            EpisodeId = episode.Id
        };

        await dbContext.TopicEpisodes.AddAsync(topicEpisode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return topicEpisode;
    }
}
