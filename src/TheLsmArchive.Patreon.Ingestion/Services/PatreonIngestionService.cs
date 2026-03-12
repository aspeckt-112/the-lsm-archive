using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Registry;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for orchestrating Patreon RSS feed ingestion and processing.
/// </summary>
public sealed class PatreonIngestionService : BackgroundService
{
    private readonly ILogger<PatreonIngestionService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly PatreonRssParser _rssParser;
    private readonly IAiSummaryService _aiSummaryService;
    private readonly ReadWriteDbContext _readWriteDbContext;
    private readonly ResiliencePipeline _aiSummaryPipeline;
    private readonly List<RssFeedSource> _sources;

    public PatreonIngestionService(
        ILogger<PatreonIngestionService> logger,
        IHostApplicationLifetime appLifetime,
        PatreonRssParser rssParser,
        IAiSummaryService aiSummaryService,
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptions<RssFeedSources> feedOptions,
        ReadWriteDbContext readWriteDbContext)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _rssParser = rssParser;
        _aiSummaryService = aiSummaryService;
        _readWriteDbContext = readWriteDbContext;
        _aiSummaryPipeline = pipelineProvider.GetPipeline(Constants.AiSummaryPipelineName);
        _sources = feedOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (PatreonFeed feed in _rssParser.ParseFeedsAsync(_sources, stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing feed '{FeedTitle}'", feed.Title);

                ShowEntity showEntity = await GetOrCreateShowAsync(feed.Title, stoppingToken);

                await IngestPostsAsync(showEntity, feed, stoppingToken);

                // Get all posts needing processing (new posts + posts with previous errors)
                List<PatreonPostEntity> postsToProcess =
                    await GetPostsNeedingProcessingAsync(showEntity.Id, stoppingToken);

                // Log retry attempts
                int retryCount = postsToProcess.Count(p => p.ProcessingError != null);

                if (retryCount > 0)
                {
                    _logger.LogInformation(
                        "Retrying {RetryCount} posts with previous processing errors",
                        retryCount);
                }

                // Process each post sequentially
                int successCount = 0;
                int errorCount = 0;

                foreach (PatreonPostEntity post in postsToProcess)
                {
                    try
                    {
                        await ProcessPostAsync(showEntity, post, stoppingToken);
                        successCount++;
                    }
                    catch (Exception postEx)
                    {
                        _logger.LogError(postEx, "Failed to process post '{PostTitle}'", post.Title);
                        errorCount++;
                    }
                }

                // Only update last synced timestamp if all posts were processed successfully
                if (errorCount == 0)
                {
                    await UpdateShowLastSyncedAtAsync(showEntity, stoppingToken);
                }
                else
                {
                    _logger.LogWarning(
                        "Skipping LastSyncedAt update for show '{ShowName}' due to {ErrorCount} failed posts",
                        showEntity.Name,
                        errorCount);
                }

                _logger.LogInformation(
                    "Completed processing feed '{FeedTitle}': {SuccessCount} successful, {ErrorCount} failed",
                    feed.Title,
                    successCount,
                    errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing feed '{FeedTitle}'", feed.Title);
            }
        }

        _appLifetime.StopApplication();
    }

    private async Task ProcessPostAsync(
        ShowEntity showEntity,
        PatreonPostEntity postEntity,
        CancellationToken cancellationToken)
    {
        bool isRetry = postEntity.ProcessingError != null;

        if (isRetry)
        {
            _logger.LogInformation(
                "Retrying processing for post '{PostTitle}' (previous error: {Error})",
                postEntity.Title,
                postEntity.ProcessingError);
        }

        // Gather context and generate AI summary outside the execution strategy so
        // transient-retry doesn't re-invoke the expensive Gemini call.
        ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

        (List<string> knownPersons, List<string> knownTopics) =
            await GetKnownContextAsync(showEntity.Id, cancellationToken);

        AiSummary aiSummary;

        try
        {
            aiSummary = await _aiSummaryPipeline.ExecuteAsync(
                context => new ValueTask<AiSummary>(
                    _aiSummaryService.GenerateAiSummaryFromPatreonPost(
                        showEntity,
                        postEntity,
                        context.CancellationToken,
                        knownPersons,
                        knownTopics)),
                resilienceContext);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(resilienceContext);
        }

        // Wrap all DB mutations in the execution strategy so the
        // NpgsqlRetryingExecutionStrategy can retry transient failures.
        IExecutionStrategy strategy = _readWriteDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _readWriteDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Create the episode
                EpisodeEntity episodeEntity =
                    await GetOrCreateEpisodeAsync(showEntity, postEntity, cancellationToken);

                // Resolve all unique persons, deduplicated by normalized name to avoid
                // creating duplicate entities when the AI returns case/spelling variants.
                var resolvedPersons = new Dictionary<string, PersonEntity>();

                foreach (string personName in aiSummary.Hosts.Concat(aiSummary.Guests))
                {
                    string normalized = NormalizeLookupKey(personName);

                    if (resolvedPersons.ContainsKey(normalized))
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing person '{Person}'", personName);
                    resolvedPersons[normalized] =
                        await GetOrCreatePersonAsync(personName, cancellationToken);
                }

                // Resolve all unique topics, same deduplication strategy.
                var resolvedTopics = new Dictionary<string, TopicEntity>();

                foreach (string topicName in aiSummary.Topics)
                {
                    string normalized = NormalizeLookupKey(topicName);

                    if (resolvedTopics.ContainsKey(normalized))
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing topic '{Topic}'", topicName);
                    resolvedTopics[normalized] =
                        await GetOrCreateTopicAsync(topicName, cancellationToken);
                }

                // Clear any previous processing error
                postEntity.ProcessingError = null;

                // Flush new entities so they receive database-generated IDs.
                await _readWriteDbContext.SaveChangesAsync(cancellationToken);

                // Upsert all relationships via INSERT … ON CONFLICT DO NOTHING.
                // This is idempotent — duplicates from fuzzy-match collisions or
                // retries are silently ignored by the database.
                foreach (PersonEntity person in resolvedPersons.Values)
                {
                    await _readWriteDbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                         INSERT INTO person_episodes (person_id, episode_id)
                         VALUES ({person.Id}, {episodeEntity.Id})
                         ON CONFLICT (person_id, episode_id) DO NOTHING
                         """,
                        cancellationToken);
                }

                foreach (TopicEntity topic in resolvedTopics.Values)
                {
                    await _readWriteDbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                         INSERT INTO topic_episodes (topic_id, episode_id)
                         VALUES ({topic.Id}, {episodeEntity.Id})
                         ON CONFLICT (topic_id, episode_id) DO NOTHING
                         """,
                        cancellationToken);
                }

                foreach (PersonEntity person in resolvedPersons.Values)
                {
                    foreach (TopicEntity topic in resolvedTopics.Values)
                    {
                        await _readWriteDbContext.Database.ExecuteSqlInterpolatedAsync(
                            $"""
                             INSERT INTO person_topics (person_id, topic_id)
                             VALUES ({person.Id}, {topic.Id})
                             ON CONFLICT (person_id, topic_id) DO NOTHING
                             """,
                            cancellationToken);
                    }
                }

                // Set the episode ID on the post (episode.Id is now generated)
                postEntity.EpisodeId = episodeEntity.Id;

                // Save the updated post and commit the transaction
                await _readWriteDbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully processed post '{PostTitle}'", postEntity.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process post '{PostTitle}'. Error will be saved for retry.",
                    postEntity.Title);

                // Rollback the transaction
                await transaction.RollbackAsync(cancellationToken);

                // Record the error via raw SQL, bypassing the change tracker.
                // After a rollback the tracker may contain entities in Added state
                // (e.g. a new EpisodeEntity created before the first SaveChangesAsync).
                // Using SaveChangesAsync here would inadvertently persist those stale
                // entities outside the transaction.
                postEntity.ProcessingError = ex.Message;
                await _readWriteDbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE patreon_posts SET processing_error = {ex.Message} WHERE id = {postEntity.Id}",
                    cancellationToken);

                throw;
            }
        });
    }

    private async Task<ShowEntity> GetOrCreateShowAsync(string name, CancellationToken cancellationToken)
    {
        ShowEntity? existingShowEntity = await _readWriteDbContext.Shows
            .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name), cancellationToken);

        if (existingShowEntity is not null)
        {
            return existingShowEntity;
        }

        _logger.LogInformation("Show '{ShowName}' not found. Creating new show record.", name);

        ShowEntity newShowEntity = new() { Name = name };

        await _readWriteDbContext.Shows.AddAsync(newShowEntity, cancellationToken);
        await _readWriteDbContext.SaveChangesAsync(cancellationToken);

        return newShowEntity;
    }

    private async Task IngestPostsAsync(
        ShowEntity showEntity,
        PatreonFeed feed,
        CancellationToken cancellationToken)
    {
        // Load all existing post IDs for this show to avoid N+1 queries
        HashSet<int> existingPatreonIds = await _readWriteDbContext.PatreonPosts
            .Where(x => x.ShowId == showEntity.Id)
            .Select(x => x.PatreonId)
            .ToHashSetAsync(cancellationToken);

        foreach (PatreonPost feedPost in feed.Posts)
        {
            if (existingPatreonIds.Contains(feedPost.Id))
            {
                continue;
            }

            PatreonPostEntity postEntity = feedPost.ToEntity(showEntity.Id);
            await _readWriteDbContext.PatreonPosts.AddAsync(postEntity, cancellationToken);
        }

        await _readWriteDbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<List<PatreonPostEntity>> GetPostsNeedingProcessingAsync(
        int showId,
        CancellationToken cancellationToken)
    {
        return _readWriteDbContext.PatreonPosts
            .Where(p => p.ShowId == showId && (p.ProcessingError != null || p.EpisodeId == null))
            .ToListAsync(cancellationToken);
    }

    private async Task<EpisodeEntity> GetOrCreateEpisodeAsync(ShowEntity showEntity,
        PatreonPostEntity post, CancellationToken cancellationToken)
    {
        EpisodeEntity? existingEpisodeEntity = await _readWriteDbContext.Episodes
            .FirstOrDefaultAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        if (existingEpisodeEntity is not null)
        {
            _logger.LogInformation(
                "Episode for post '{PostTitle}' already exists with ID {EpisodeId}",
                post.Title,
                existingEpisodeEntity.Id);

            return existingEpisodeEntity;
        }

        var episodeEntity = new EpisodeEntity
        {
            Show = showEntity,
            Title = post.Title,
            ReleaseDateUtc = post.Published.UtcDateTime,
            PatreonPost = post
        };

        _readWriteDbContext.Episodes.Add(episodeEntity);

        return episodeEntity;
    }

    private async Task<(List<string> Persons, List<string> Topics)> GetKnownContextAsync(
        int showId,
        CancellationToken cancellationToken)
    {
        // 1. Get persons associated with this show (hosts/frequent guests)
        List<string> persons = await _readWriteDbContext.PersonEpisodes
            .Where(pe => pe.Episode.ShowId == showId)
            .GroupBy(pe => pe.Person.Name)
            .OrderByDescending(g => g.Count())
            .Take(50) // 50 because Summon Sign has a lot of unique guests.
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        // 2. Get recent topics for this show (context for ongoing discussions)
        List<string> topics = await _readWriteDbContext.TopicEpisodes
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

    private async Task<PersonEntity> GetOrCreatePersonAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = NormalizeLookupKey(name);

        // 1. Try exact match first using canonical normalized form.
        PersonEntity? personEntity = await _readWriteDbContext.Persons
            .FirstOrDefaultAsync(p => p.NormalizedName == normalizedName, cancellationToken);

        if (personEntity is not null)
        {
            _logger.LogInformation(
                "Person '{Person}' already exists in the database with ID {PersonEntityId} (normalized key: {NormalizedName})",
                name,
                personEntity.Id,
                normalizedName);

            return personEntity;
        }

        // 2. Try fuzzy match (Postgres trigram similarity)
        personEntity = await _readWriteDbContext.Persons
            .Where(p => EF.Functions.TrigramsSimilarity(p.Name, name) > 0.8)
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, name))
            .FirstOrDefaultAsync(cancellationToken);

        if (personEntity is not null)
        {
            _logger.LogInformation(
                "Fuzzy matched person '{InputName}' to existing person '{MatchedName}' (ID {PersonEntityId})",
                name,
                personEntity.Name,
                personEntity.Id);

            return personEntity;
        }

        personEntity = new PersonEntity { Name = name, NormalizedName = normalizedName };
        _readWriteDbContext.Persons.Add(personEntity);

        return personEntity;
    }

    private async Task<TopicEntity> GetOrCreateTopicAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = NormalizeLookupKey(name);

        // 1. Try exact match first using canonical normalized form.
        TopicEntity? topicEntity = await _readWriteDbContext.Topics
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName, cancellationToken);

        if (topicEntity is not null)
        {
            _logger.LogInformation(
                "Topic '{TopicName}' already exists in the database with ID {TopicEntityId} (normalized key: {NormalizedName})",
                name,
                topicEntity.Id,
                normalizedName);

            return topicEntity;
        }

        // 2. Try fuzzy match (Postgres trigram similarity)
        // A threshold of 0.8 ensures high similarity while allowing for punctuation/typo differences.
        // This prevents distinct topics like "Game Name" and "Game Name Remaster" from being merged.
        topicEntity = await _readWriteDbContext.Topics
            .Where(t => EF.Functions.TrigramsSimilarity(t.Name, name) > 0.8)
            .OrderByDescending(t => EF.Functions.TrigramsSimilarity(t.Name, name))
            .FirstOrDefaultAsync(cancellationToken);

        if (topicEntity is not null)
        {
            _logger.LogInformation(
                "Fuzzy matched topic '{InputName}' to existing topic '{MatchedName}' (ID {TopicEntityId})",
                name,
                topicEntity.Name,
                topicEntity.Id);

            return topicEntity;
        }

        topicEntity = new TopicEntity { Name = name, NormalizedName = normalizedName };
        _readWriteDbContext.Topics.Add(topicEntity);

        return topicEntity;
    }

    private async Task UpdateShowLastSyncedAtAsync(ShowEntity showEntity, CancellationToken cancellationToken)
    {
        showEntity.LastSyncedAt = DateTimeOffset.UtcNow;
        await _readWriteDbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeLookupKey(string value)
    {
        string trimmed = value.Trim();

        char[] alphanumericLowered =
        [
            .. trimmed
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
        ];

        return alphanumericLowered.Length == 0
            ? trimmed.ToLowerInvariant()
            : new string(alphanumericLowered);
    }
}
