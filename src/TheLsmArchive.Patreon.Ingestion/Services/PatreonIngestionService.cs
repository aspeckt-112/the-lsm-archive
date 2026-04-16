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
    private sealed record ShowReference(int Id, string Name);

    private sealed record PendingPost(int Id, string Title, string? ProcessingError);

    private readonly ILogger<PatreonIngestionService> _logger;
    private readonly PatreonRssParser _rssParser;
    private readonly IAiSummaryService _aiSummaryService;
    private readonly IDbContextFactory<LsmArchiveDbContext> _dbContextFactory;
    private readonly ResiliencePipeline _aiSummaryPipeline;
    private readonly List<RssFeedSource> _sources;
    private readonly TimeSpan _ingestionInterval;

    public PatreonIngestionService(
        ILogger<PatreonIngestionService> logger,
        PatreonRssParser rssParser,
        IAiSummaryService aiSummaryService,
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptions<RssFeedSources> feedOptions,
        IOptions<PatreonIngestionOptions> ingestionOptions,
        IDbContextFactory<LsmArchiveDbContext> dbContextFactory)
    {
        _logger = logger;
        _rssParser = rssParser;
        _aiSummaryService = aiSummaryService;
        _dbContextFactory = dbContextFactory;
        _aiSummaryPipeline = pipelineProvider.GetPipeline(Constants.AiSummaryPipelineName);
        _sources = feedOptions.Value;
        _ingestionInterval = TimeSpan.FromMinutes(ingestionOptions.Value.RefreshIntervalInMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteIngestionCycleAsync(stoppingToken);

            try
            {
                await Task.Delay(_ingestionInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The host is shutting down.
                break;
            }
        }
    }

    /// <summary>
    /// Executes a single ingestion cycle: parses all configured RSS feeds, ingests new posts,
    /// and processes them through the AI summary pipeline.
    /// </summary>
    internal async Task ExecuteIngestionCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting Patreon ingestion cycle. Next run in {IntervalMinutes} minutes.",
            _ingestionInterval.TotalMinutes);

        try
        {
            await foreach (PatreonFeed feed in _rssParser.ParseFeedsAsync(_sources, cancellationToken))
            {
                try
                {
                    _logger.LogInformation("Processing feed '{FeedTitle}'", feed.Title);

                    ShowReference show = await GetOrCreateShowAsync(feed.Title, cancellationToken);

                    await IngestPostsAsync(show.Id, feed, cancellationToken);

                    // Get all posts needing processing (new posts + posts with previous errors)
                    List<PendingPost> postsToProcess =
                        await GetPostsNeedingProcessingAsync(show.Id, cancellationToken);

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

                    foreach (PendingPost post in postsToProcess)
                    {
                        try
                        {
                            await ProcessPostAsync(show, post, cancellationToken);
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
                        await UpdateShowLastSyncedAtAsync(show.Id, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Skipping LastSyncedAt update for show '{ShowName}' due to {ErrorCount} failed posts",
                            show.Name,
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Patreon ingestion cycle");
        }
    }

    private async Task ProcessPostAsync(
        ShowReference show,
        PendingPost post,
        CancellationToken cancellationToken)
    {
        bool isRetry = post.ProcessingError is not null;

        if (isRetry)
        {
            _logger.LogInformation(
                "Retrying processing for post '{PostTitle}' (previous error: {Error})",
                post.Title,
                post.ProcessingError);
        }

        await using LsmArchiveDbContext contextForAi = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        PatreonPostEntity postEntity = await contextForAi.PatreonPosts
            .AsNoTracking()
            .Include(p => p.Show)
            .FirstOrDefaultAsync(p => p.Id == post.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Patreon post with ID {post.Id} was not found.");

        ShowEntity showEntity = postEntity.Show;

        // Gather context and generate AI summary outside the execution strategy so
        // transient-retry doesn't re-invoke the expensive Gemini call.
        ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

        (List<string> knownPersons, List<string> knownTopics) =
            await GetKnownContextAsync(show.Id, cancellationToken);

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
        await using LsmArchiveDbContext strategyContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                PatreonPostEntity trackedPostEntity = await dbContext.PatreonPosts
                    .Include(p => p.Show)
                    .FirstOrDefaultAsync(p => p.Id == post.Id, cancellationToken)
                    ?? throw new InvalidOperationException($"Patreon post with ID {post.Id} was not found.");

                // Create the episode
                EpisodeEntity episodeEntity =
                    await GetOrCreateEpisodeAsync(dbContext, trackedPostEntity, cancellationToken);

                // Resolve all unique persons, deduplicated by normalized name to avoid
                // creating duplicate entities when the AI returns case/spelling variants.
                var resolvedPersons = new Dictionary<string, PersonEntity>();

                foreach (string personName in aiSummary.Hosts.Concat(aiSummary.Guests))
                {
                    string normalized = LookupKeyNormalizer.Normalize(personName);

                    if (resolvedPersons.ContainsKey(normalized))
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing person '{Person}'", personName);
                    resolvedPersons[normalized] =
                        await GetOrCreatePersonAsync(dbContext, personName, cancellationToken);
                }

                // Resolve all unique topics, same deduplication strategy.
                var resolvedTopics = new Dictionary<string, TopicEntity>();

                foreach (string topicName in aiSummary.Topics)
                {
                    string normalized = LookupKeyNormalizer.Normalize(topicName);

                    if (resolvedTopics.ContainsKey(normalized))
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing topic '{Topic}'", topicName);
                    resolvedTopics[normalized] =
                        await GetOrCreateTopicAsync(dbContext, topicName, cancellationToken);
                }

                // Clear any previous processing error
                trackedPostEntity.ProcessingError = null;

                // Flush new entities so they receive database-generated IDs.
                await dbContext.SaveChangesAsync(cancellationToken);

                // Upsert all relationships via INSERT … ON CONFLICT DO NOTHING.
                // This is idempotent — duplicates from fuzzy-match collisions or
                // retries are silently ignored by the database.
                foreach (PersonEntity person in resolvedPersons.Values)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                         INSERT INTO person_episodes (person_id, episode_id)
                         VALUES ({person.Id}, {episodeEntity.Id})
                         ON CONFLICT (person_id, episode_id) DO NOTHING
                         """,
                        cancellationToken);
                }

                foreach (TopicEntity topic in resolvedTopics.Values)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
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
                        await dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $"""
                             INSERT INTO person_topics (person_id, topic_id)
                             VALUES ({person.Id}, {topic.Id})
                             ON CONFLICT (person_id, topic_id) DO NOTHING
                             """,
                            cancellationToken);
                    }
                }

                // Set the episode ID on the post (episode.Id is now generated)
                trackedPostEntity.EpisodeId = episodeEntity.Id;

                // Save the updated post and commit the transaction
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully processed post '{PostTitle}'", post.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process post '{PostTitle}'. Error will be saved for retry.",
                    post.Title);

                // Rollback the transaction
                await transaction.RollbackAsync(cancellationToken);

                await SaveProcessingErrorAsync(post.Id, ex.Message, cancellationToken);

                throw;
            }
        });
    }

    private async Task<ShowReference> GetOrCreateShowAsync(string name, CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        ShowEntity? existingShowEntity = await dbContext.Shows
            .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name), cancellationToken);

        if (existingShowEntity is not null)
        {
            return new ShowReference(existingShowEntity.Id, existingShowEntity.Name);
        }

        _logger.LogInformation("Show '{ShowName}' not found. Creating new show record.", name);

        ShowEntity newShowEntity = new() { Name = name };

        await dbContext.Shows.AddAsync(newShowEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShowReference(newShowEntity.Id, newShowEntity.Name);
    }

    private async Task IngestPostsAsync(
        int showId,
        PatreonFeed feed,
        CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Load all existing post IDs for this show to avoid N+1 queries
        HashSet<int> existingPatreonIds = await dbContext.PatreonPosts
            .Where(x => x.ShowId == showId)
            .Select(x => x.PatreonId)
            .ToHashSetAsync(cancellationToken);

        foreach (PatreonPost feedPost in feed.Posts)
        {
            if (existingPatreonIds.Contains(feedPost.Id))
            {
                continue;
            }

            PatreonPostEntity postEntity = feedPost.ToEntity(showId);
            await dbContext.PatreonPosts.AddAsync(postEntity, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<PendingPost>> GetPostsNeedingProcessingAsync(
        int showId,
        CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.PatreonPosts
            .AsNoTracking()
            .Where(p => p.ShowId == showId && (p.ProcessingError != null || p.EpisodeId == null))
            .Select(p => new PendingPost(p.Id, p.Title, p.ProcessingError))
            .ToListAsync(cancellationToken);
    }

    private async Task<EpisodeEntity> GetOrCreateEpisodeAsync(
        LsmArchiveDbContext dbContext,
        PatreonPostEntity post,
        CancellationToken cancellationToken)
    {
        EpisodeEntity? existingEpisodeEntity = await dbContext.Episodes
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
            ShowId = post.ShowId,
            Title = post.Title,
            ReleaseDateUtc = post.Published.UtcDateTime,
            PatreonPost = post
        };

        dbContext.Episodes.Add(episodeEntity);

        return episodeEntity;
    }

    private async Task<(List<string> Persons, List<string> Topics)> GetKnownContextAsync(
        int showId,
        CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // 1. Get persons associated with this show (hosts/frequent guests)
        List<string> persons = await dbContext.PersonEpisodes
            .Where(pe => pe.Episode.ShowId == showId)
            .GroupBy(pe => pe.Person.Name)
            .OrderByDescending(g => g.Count())
            .Take(50) // 50 because Summon Sign has a lot of unique guests.
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        // 2. Get recent topics for this show (context for ongoing discussions)
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

    private async Task<PersonEntity> GetOrCreatePersonAsync(
        LsmArchiveDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        // 1. Try exact match first using canonical normalized form.
        PersonEntity? personEntity = await dbContext.Persons
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
        personEntity = await dbContext.Persons
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
        dbContext.Persons.Add(personEntity);

        return personEntity;
    }

    private async Task<TopicEntity> GetOrCreateTopicAsync(
        LsmArchiveDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        // 1. Try exact match first using canonical normalized form.
        TopicEntity? topicEntity = await dbContext.Topics
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
        topicEntity = await dbContext.Topics
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
        dbContext.Topics.Add(topicEntity);

        return topicEntity;
    }

    private async Task SaveProcessingErrorAsync(int postId, string errorMessage, CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        PatreonPostEntity postEntity = await dbContext.PatreonPosts
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken)
            ?? throw new InvalidOperationException($"Patreon post with ID {postId} was not found.");

        postEntity.ProcessingError = errorMessage;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateShowLastSyncedAtAsync(int showId, CancellationToken cancellationToken)
    {
        await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        ShowEntity showEntity = await dbContext.Shows
            .FirstOrDefaultAsync(show => show.Id == showId, cancellationToken)
            ?? throw new InvalidOperationException($"Show with ID {showId} was not found.");

        showEntity.LastSyncedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
