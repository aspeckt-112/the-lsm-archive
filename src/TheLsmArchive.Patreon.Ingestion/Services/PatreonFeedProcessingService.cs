using System.Collections.Immutable;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Helpers;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for Patreon feed ingestion, pending-post discovery,
/// and per-post processing.
/// </summary>
public sealed partial class PatreonFeedProcessingService : IPatreonFeedProcessingService
{
    private readonly ILogger<PatreonFeedProcessingService> _logger;
    private readonly IDbContextFactory<LsmArchiveDbContext> _dbContextFactory;
    private readonly IMetadataExtractionService _metadataExtractionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonFeedProcessingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="metadataExtractionService">The metadata extraction service.</param>
    public PatreonFeedProcessingService(
        ILogger<PatreonFeedProcessingService> logger,
        IDbContextFactory<LsmArchiveDbContext> dbContextFactory,
        IMetadataExtractionService metadataExtractionService)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _metadataExtractionService = metadataExtractionService;
    }

    /// <summary>
    /// Processes a single Patreon feed from show resolution through pending-post processing.
    /// </summary>
    /// <param name="feed">The Patreon feed to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ProcessFeedAsync(PatreonFeed feed, CancellationToken cancellationToken)
    {
        int showId = await GetOrCreateShowAsync(feed.Title, cancellationToken);

        LogIngestingFeed(feed.Title, showId);

        ImmutableList<PendingPost> postsToProcess = await GetPendingPostsAsync(showId, cancellationToken);

        int retryCount = postsToProcess.Count(p => p.ProcessingError is not null);

        if (retryCount > 0)
        {
            LogRetryingPostsWithErrors(retryCount);
        }

        int successCount = 0;
        int errorCount = 0;

        foreach (PendingPost post in postsToProcess)
        {
            try
            {
                await ProcessPendingPostAsync(showId, post, cancellationToken);
                successCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception postEx)
            {
                LogProcessPostFailed(postEx, post.Title);
                errorCount++;
            }
        }

        LogCompletedFeedProcessing(feed.Title, successCount, errorCount);
    }

    /// <summary>
    /// Gets the show by name or creates a new show if it does not exist, and returns the show ID.
    /// </summary>
    /// <param name="name">The name of the show.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the existing or newly created show.</returns>
    public async Task<int> GetOrCreateShowAsync(string name, CancellationToken cancellationToken)
    {
        LogAttemptingGetOrCreateShow(name);

        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        ShowEntity? existingShowEntity = await dbContext.Shows
            .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name), cancellationToken);

        if (existingShowEntity is not null)
        {
            LogShowFound(name, existingShowEntity.Id);
            return existingShowEntity.Id;
        }

        LogShowNotFound(name);

        ShowEntity newShowEntity = new() { Name = name };

        await dbContext.Shows.AddAsync(newShowEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogShowCreated(name, newShowEntity.Id);

        return newShowEntity.Id;
    }

    /// <summary>
    /// Ingests the given Patreon feed for the specified show ID, adding new posts to the database while avoiding duplicates based on Patreon post IDs.
    /// </summary>
    /// <param name="showId">The ID of the show associated with the Patreon feed.</param>
    /// <param name="feed">The Patreon feed to ingest.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task IngestFeedAsync(int showId, PatreonFeed feed, CancellationToken cancellationToken)
    {
        LogIngestingFeed(feed.Title, showId);

        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        await ThrowIfShowDoesNotExistAsync(dbContext, showId, cancellationToken);

        HashSet<int> existingPostIds = await dbContext.PatreonPosts
            .AsNoTracking()
            .Where(p => p.ShowId == showId)
            .Select(p => p.PatreonId)
            .ToHashSetAsync(cancellationToken);

        foreach (PatreonPost post in feed.Posts)
        {
            if (existingPostIds.Contains(post.Id))
            {
                LogPostAlreadyExists(post.Id, showId);
                continue;
            }

            PatreonPostEntity postEntity = post.ToEntity(showId);
            await dbContext.PatreonPosts.AddAsync(postEntity, cancellationToken);
            LogAddedNewPost(post.Id, showId);
        }

        LogSavingFeedChanges(feed.Title, showId);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogFinishedIngestingFeed(feed.Title, showId);
    }

    /// <summary>
    /// Retrieves the pending posts for the specified show ID.
    /// </summary>
    /// <param name="showId">The ID of the show.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pending posts for the show.</returns>
    public async Task<ImmutableList<PendingPost>> GetPendingPostsAsync(int showId, CancellationToken cancellationToken)
    {
        LogRetrievingPendingPosts(showId);

        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        await ThrowIfShowDoesNotExistAsync(dbContext, showId, cancellationToken);

        List<PendingPost> pendingPosts = await dbContext.PatreonPosts
            .AsNoTracking()
            .Where(p => p.ShowId == showId && (p.ProcessingError != null || p.EpisodeId == null))
            .Select(p => new PendingPost(p.Id, p.Title, p.ProcessingError))
            .ToListAsync(cancellationToken);

        LogRetrievedPendingPosts(pendingPosts.Count, showId);

        return [.. pendingPosts];
    }

    /// <summary>
    /// Processes the given pending post: invokes metadata extraction, then persists the
    /// resulting episode, persons, topics, and relationships in a single transaction.
    /// On failure, the error message is saved to the post for retry on the next ingestion cycle.
    /// </summary>
    /// <param name="showId">The ID of the show the post belongs to.</param>
    /// <param name="pendingPost">The pending post to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ProcessPendingPostAsync(int showId, PendingPost pendingPost, CancellationToken cancellationToken)
    {
        bool isRetry = pendingPost.ProcessingError is not null;

        PatreonPostEntity postEntity;
        ShowEntity showEntity;
        List<string> knownPersons;
        List<string> knownTopics;

        if (isRetry)
        {
            LogRetryingPost(pendingPost.Title, pendingPost.ProcessingError);
        }

        await using (LsmArchiveDbContext setupDbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            postEntity = await setupDbContext.PatreonPosts
                .AsNoTracking()
                .Include(p => p.Show)
                .FirstOrDefaultAsync(p => p.Id == pendingPost.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Patreon post with ID {pendingPost.Id} was not found.");

            showEntity = postEntity.Show;

            (knownPersons, knownTopics) = await GetKnownContextAsync(setupDbContext, showId, cancellationToken);
        }

        AiSummary aiSummary = await _metadataExtractionService.ExtractMetadataAsync(
            showEntity,
            postEntity,
            cancellationToken,
            knownPersons,
            knownTopics);

        await using LsmArchiveDbContext strategyDbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        IExecutionStrategy strategy = strategyDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                int episodeId = await GetOrCreateEpisodeAsync(dbContext, postEntity, cancellationToken);

                var seenPersonKeys = new HashSet<string>();
                var personIds = new List<int>();

                foreach (string name in aiSummary.Hosts.Concat(aiSummary.Guests))
                {
                    string key = LookupKeyNormalizer.Normalize(name);

                    if (!seenPersonKeys.Add(key))
                    {
                        continue;
                    }

                    LogProcessingPerson(name);
                    personIds.Add(await GetOrCreatePersonAsync(dbContext, name, cancellationToken));
                }

                var seenTopicKeys = new HashSet<string>();
                var topicIds = new List<int>();

                foreach (string name in aiSummary.Topics)
                {
                    string key = LookupKeyNormalizer.Normalize(name);

                    if (!seenTopicKeys.Add(key))
                    {
                        continue;
                    }

                    LogProcessingTopic(name);
                    topicIds.Add(await GetOrCreateTopicAsync(dbContext, name, cancellationToken));
                }

                await LinkPersonsToEpisodeAsync(dbContext, personIds, episodeId, cancellationToken);
                await LinkTopicsToEpisodeAsync(dbContext, topicIds, episodeId, cancellationToken);
                await LinkPersonsToTopicsAsync(dbContext, personIds, topicIds, cancellationToken);

                await MarkPostAsProcessedAsync(dbContext, pendingPost.Id, episodeId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                LogPostProcessingSuccess(pendingPost.Title);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            catch (Exception ex)
            {
                LogPostProcessingFailedSaving(ex, pendingPost.Title);

                await transaction.RollbackAsync(cancellationToken);

                await SaveProcessingErrorAsync(pendingPost.Id, ex.Message, cancellationToken);

                throw;
            }
        });
    }

    private async Task<int> GetOrCreateEpisodeAsync(
        LsmArchiveDbContext dbContext,
        PatreonPostEntity post,
        CancellationToken cancellationToken)
    {
        EpisodeEntity? existingEpisode = await dbContext.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        if (existingEpisode is not null)
        {
            LogEpisodeAlreadyExists(post.Title, existingEpisode.Id);
            return existingEpisode.Id;
        }

        EpisodeEntity episode = new()
        {
            ShowId = post.ShowId,
            Title = post.Title,
            ReleaseDateUtc = post.Published.UtcDateTime,
            PatreonPostId = post.Id
        };

        dbContext.Episodes.Add(episode);
        await dbContext.SaveChangesAsync(cancellationToken);

        return episode.Id;
    }

    private async Task<int> GetOrCreatePersonAsync(
        LsmArchiveDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        PersonEntity? person = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.NormalizedName == normalizedName, cancellationToken);

        if (person is not null)
        {
            LogPersonAlreadyExists(name, person.Id, normalizedName);
            return person.Id;
        }

        person = await dbContext.Persons
            .AsNoTracking()
            .Where(p => EF.Functions.TrigramsSimilarity(p.Name, name) > 0.8)
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, name))
            .FirstOrDefaultAsync(cancellationToken);

        if (person is not null)
        {
            LogFuzzyMatchedPerson(name, person.Name, person.Id);
            return person.Id;
        }

        person = new PersonEntity { Name = name, NormalizedName = normalizedName };
        dbContext.Persons.Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);

        return person.Id;
    }

    private async Task<int> GetOrCreateTopicAsync(
        LsmArchiveDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        TopicEntity? topic = await dbContext.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName, cancellationToken);

        if (topic is not null)
        {
            LogTopicAlreadyExists(name, topic.Id, normalizedName);
            return topic.Id;
        }

        topic = await dbContext.Topics
            .AsNoTracking()
            .Where(t => EF.Functions.TrigramsSimilarity(t.Name, name) > 0.8)
            .OrderByDescending(t => EF.Functions.TrigramsSimilarity(t.Name, name))
            .FirstOrDefaultAsync(cancellationToken);

        if (topic is not null)
        {
            LogFuzzyMatchedTopic(name, topic.Name, topic.Id);
            return topic.Id;
        }

        topic = new TopicEntity { Name = name, NormalizedName = normalizedName };
        dbContext.Topics.Add(topic);
        await dbContext.SaveChangesAsync(cancellationToken);

        return topic.Id;
    }

    private static async Task LinkPersonsToEpisodeAsync(
        LsmArchiveDbContext dbContext,
        IReadOnlyCollection<int> personIds,
        int episodeId,
        CancellationToken cancellationToken)
    {
        if (personIds.Count == 0)
        {
            return;
        }

        HashSet<int> existingPersonIds = await dbContext.PersonEpisodes
            .Where(pe => pe.EpisodeId == episodeId)
            .Select(pe => pe.PersonId)
            .ToHashSetAsync(cancellationToken);

        foreach (int personId in personIds)
        {
            if (existingPersonIds.Contains(personId))
            {
                continue;
            }

            dbContext.PersonEpisodes.Add(new PersonEpisodeEntity { PersonId = personId, EpisodeId = episodeId });
        }
    }

    private static async Task LinkTopicsToEpisodeAsync(
        LsmArchiveDbContext dbContext,
        IReadOnlyCollection<int> topicIds,
        int episodeId,
        CancellationToken cancellationToken)
    {
        if (topicIds.Count == 0)
        {
            return;
        }

        HashSet<int> existingTopicIds = await dbContext.TopicEpisodes
            .Where(te => te.EpisodeId == episodeId)
            .Select(te => te.TopicId)
            .ToHashSetAsync(cancellationToken);

        foreach (int topicId in topicIds)
        {
            if (existingTopicIds.Contains(topicId))
            {
                continue;
            }

            dbContext.TopicEpisodes.Add(new TopicEpisodeEntity { TopicId = topicId, EpisodeId = episodeId });
        }
    }

    private static async Task LinkPersonsToTopicsAsync(
        LsmArchiveDbContext dbContext,
        IReadOnlyCollection<int> personIds,
        IReadOnlyCollection<int> topicIds,
        CancellationToken cancellationToken)
    {
        if (personIds.Count == 0 || topicIds.Count == 0)
        {
            return;
        }

        var existingLinkRows = await dbContext.PersonTopics
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

                dbContext.PersonTopics.Add(new PersonTopicEntity { PersonId = personId, TopicId = topicId });
            }
        }
    }

    private async Task MarkPostAsProcessedAsync(
        LsmArchiveDbContext dbContext,
        int postId,
        int episodeId,
        CancellationToken cancellationToken)
    {
        PatreonPostEntity postEntity = await dbContext.PatreonPosts
                                       .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken)
                                   ?? throw new InvalidOperationException(
                                       $"Patreon post with ID {postId} was not found.");

        postEntity.EpisodeId = episodeId;
        postEntity.ProcessingError = null;
    }

    private async Task SaveProcessingErrorAsync(int postId, string errorMessage, CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        PatreonPostEntity postEntity = await dbContext.PatreonPosts
                                           .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken)
                                       ?? throw new InvalidOperationException(
                                           $"Patreon post with ID {postId} was not found.");

        postEntity.ProcessingError = errorMessage;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ThrowIfShowDoesNotExistAsync(
        LsmArchiveDbContext dbContext,
        int showId,
        CancellationToken cancellationToken)
    {
        bool showExists = await dbContext.Shows
            .AsNoTracking()
            .AnyAsync(s => s.Id == showId, cancellationToken);

        if (!showExists)
        {
            LogShowDoesNotExist(showId);
            throw new InvalidOperationException($"Show with ID {showId} does not exist in the database.");
        }
    }

    private static async Task<(List<string> Persons, List<string> Topics)> GetKnownContextAsync(
        LsmArchiveDbContext dbContext,
        int showId,
        CancellationToken cancellationToken)
    {
        List<string> persons = await dbContext.PersonEpisodes
            .Where(pe => pe.Episode.ShowId == showId)
            .GroupBy(pe => pe.Person.Name)
            .OrderByDescending(g => g.Count())
            .Take(50)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        List<string> topics = await dbContext.TopicEpisodes
            .Where(te => te.Episode.ShowId == showId)
            .GroupBy(te => te.Topic.Name)
            .Select(g => new
            {
                TopicName = g.Key,
                LastUsedAt = g.Max(te => te.Episode.ReleaseDateUtc)
            })
            .OrderByDescending(x => x.LastUsedAt)
            .Take(150)
            .Select(x => x.TopicName)
            .ToListAsync(cancellationToken);

        return (persons, topics);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingesting feed '{FeedTitle}' for show ID {ShowId}")]
    private partial void LogIngestingFeed(string feedTitle, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrying {RetryCount} posts with previous processing errors")]
    private partial void LogRetryingPostsWithErrors(int retryCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process post '{PostTitle}'")]
    private partial void LogProcessPostFailed(Exception exception, string postTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed processing feed '{FeedTitle}': {SuccessCount} successful, {ErrorCount} failed")]
    private partial void LogCompletedFeedProcessing(string feedTitle, int successCount, int errorCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Attempting to get or create show with name '{ShowName}'.")]
    private partial void LogAttemptingGetOrCreateShow(string showName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Show '{ShowName}' found with ID {ShowId}.")]
    private partial void LogShowFound(string showName, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Show '{ShowName}' not found. Creating new show record.")]
    private partial void LogShowNotFound(string showName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Show '{ShowName}' created with ID {ShowId}.")]
    private partial void LogShowCreated(string showName, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Post with Patreon ID {PatreonId} already exists for show ID {ShowId}, skipping.")]
    private partial void LogPostAlreadyExists(int patreonId, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Added new post with Patreon ID {PatreonId} for show ID {ShowId}.")]
    private partial void LogAddedNewPost(int patreonId, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saving changes to the database for feed '{FeedTitle}' and show ID {ShowId}.")]
    private partial void LogSavingFeedChanges(string feedTitle, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Finished ingesting feed '{FeedTitle}' for show ID {ShowId}.")]
    private partial void LogFinishedIngestingFeed(string feedTitle, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieving pending posts for show ID {ShowId}.")]
    private partial void LogRetrievingPendingPosts(int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} pending posts for show ID {ShowId}.")]
    private partial void LogRetrievedPendingPosts(int count, int showId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrying processing for post '{PostTitle}' (previous error: {Error})")]
    private partial void LogRetryingPost(string postTitle, string? error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing person '{Person}'")]
    private partial void LogProcessingPerson(string person);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing topic '{Topic}'")]
    private partial void LogProcessingTopic(string topic);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully processed post '{PostTitle}'")]
    private partial void LogPostProcessingSuccess(string postTitle);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process post '{PostTitle}'. Error will be saved for retry.")]
    private partial void LogPostProcessingFailedSaving(Exception exception, string postTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Episode for post '{PostTitle}' already exists with ID {EpisodeId}")]
    private partial void LogEpisodeAlreadyExists(string postTitle, int episodeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Person '{Person}' already exists with ID {PersonId} (normalized key: {NormalizedName})")]
    private partial void LogPersonAlreadyExists(string person, int personId, string normalizedName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fuzzy matched person '{InputName}' to existing person '{MatchedName}' (ID {PersonId})")]
    private partial void LogFuzzyMatchedPerson(string inputName, string matchedName, int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Topic '{TopicName}' already exists with ID {TopicId} (normalized key: {NormalizedName})")]
    private partial void LogTopicAlreadyExists(string topicName, int topicId, string normalizedName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fuzzy matched topic '{InputName}' to existing topic '{MatchedName}' (ID {TopicId})")]
    private partial void LogFuzzyMatchedTopic(string inputName, string matchedName, int topicId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Show with ID {ShowId} does not exist in the database.")]
    private partial void LogShowDoesNotExist(int showId);
}
