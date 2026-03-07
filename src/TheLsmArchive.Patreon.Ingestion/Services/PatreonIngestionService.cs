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

        // Use the execution strategy to support retry with transactions
        IExecutionStrategy strategy = _readWriteDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _readWriteDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

                // Generate AI summary
                AiSummary aiSummary;

                try
                {
                    aiSummary = await _aiSummaryPipeline.ExecuteAsync(
                        context => new ValueTask<AiSummary>(
                            _aiSummaryService.GenerateAiSummaryFromPatreonPost(showEntity, postEntity,
                                context.CancellationToken)),
                        resilienceContext);
                }
                finally
                {
                    ResilienceContextPool.Shared.Return(resilienceContext);
                }

                // Create the episode
                EpisodeEntity episodeEntity = await GetOrCreateEpisodeAsync(showEntity, postEntity, cancellationToken);

                // Process hosts
                List<PersonEntity> personEntities = [];

                foreach (string host in aiSummary.Hosts)
                {
                    _logger.LogInformation("Processing person '{Person}'", host);

                    PersonEntity personEntity = await GetOrCreatePersonAsync(host, cancellationToken);

                    await CreatePersonEpisodeRelationshipIfNotExistsAsync(personEntity, episodeEntity,
                        cancellationToken);

                    personEntities.Add(personEntity);
                }

                // Process topics
                foreach (string topic in aiSummary.Topics)
                {
                    _logger.LogInformation("Processing topic '{Topic}'", topic);

                    TopicEntity topicEntity = await GetOrCreateTopicAsync(topic, cancellationToken);
                    await CreateTopicEpisodeRelationshipIfNotExistsAsync(topicEntity, episodeEntity, cancellationToken);

                    // Link topic to people
                    foreach (PersonEntity personEntity in personEntities)
                    {
                        await CreatePersonTopicRelationshipIfNotExistsAsync(personEntity, topicEntity,
                            cancellationToken);
                    }
                }

                // Clear any previous processing error
                postEntity.ProcessingError = null;

                // Save to generate IDs for all new entities (episode, persons, topics, etc.)
                await _readWriteDbContext.SaveChangesAsync(cancellationToken);

                // Now set the episode ID on the post (episode.Id is now generated)
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

                // Save the error in a separate operation
                postEntity.ProcessingError = ex.Message;
                await _readWriteDbContext.SaveChangesAsync(cancellationToken);

                // Re-throw to allow the execution strategy to handle retries if needed
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
            .FirstOrDefaultAsync(e => e.PatreonPost.Id == post.Id, cancellationToken);

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

    private async Task<PersonEntity> GetOrCreatePersonAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        PersonEntity? personEntity = await _readWriteDbContext.Persons
            .FirstOrDefaultAsync(p => EF.Functions.ILike(p.Name, name), cancellationToken);

        if (personEntity is not null)
        {
            _logger.LogInformation(
                "Person '{Person}' already exists in the database with ID {PersonEntityId}",
                name,
                personEntity.Id);

            return personEntity;
        }

        personEntity = new PersonEntity { Name = name };
        _readWriteDbContext.Persons.Add(personEntity);

        return personEntity;
    }

    private async Task CreatePersonEpisodeRelationshipIfNotExistsAsync(PersonEntity personEntity,
        EpisodeEntity episodeEntity, CancellationToken cancellationToken)
    {
        PersonEpisodeEntity? existingPersonEpisode = await _readWriteDbContext.PersonEpisodes
            .FirstOrDefaultAsync(
                pe => pe.Person.Id == personEntity.Id && pe.Episode.Id == episodeEntity.Id,
                cancellationToken);

        if (existingPersonEpisode is not null)
        {
            _logger.LogInformation(
                "Person '{Person}' is already associated with episode '{EpisodeTitle}'",
                personEntity.Name,
                episodeEntity.Title);

            return;
        }

        var personEpisodeEntity = new PersonEpisodeEntity { Episode = episodeEntity, Person = personEntity };
        _readWriteDbContext.PersonEpisodes.Add(personEpisodeEntity);
    }

    private async Task<TopicEntity> GetOrCreateTopicAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();

        // 1. Try exact match first (case-insensitive)
        TopicEntity? topicEntity = await _readWriteDbContext.Topics
            .FirstOrDefaultAsync(t => EF.Functions.ILike(t.Name, name), cancellationToken);

        if (topicEntity is not null)
        {
            _logger.LogInformation(
                "Topic '{TopicName}' already exists in the database with ID {TopicEntityId}",
                name,
                topicEntity.Id);

            return topicEntity;
        }

        // 2. Try fuzzy match (Postgres trigram similarity)
        // A threshold of 0.8 is quite high, ensuring we don't match completely different things.
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

        topicEntity = new TopicEntity { Name = name };
        _readWriteDbContext.Topics.Add(topicEntity);

        return topicEntity;
    }

    private async Task CreateTopicEpisodeRelationshipIfNotExistsAsync(TopicEntity topicEntity,
        EpisodeEntity episodeEntity, CancellationToken cancellationToken)
    {
        TopicEpisodeEntity? existingTopicEpisode = await _readWriteDbContext.TopicEpisodes
            .FirstOrDefaultAsync(
                te => te.Topic.Id == topicEntity.Id && te.Episode.Id == episodeEntity.Id,
                cancellationToken);

        if (existingTopicEpisode is not null)
        {
            _logger.LogInformation(
                "Topic '{TopicName}' is already associated with episode '{EpisodeTitle}'",
                topicEntity.Name,
                episodeEntity.Title);

            return;
        }

        var topicEpisodeEntity = new TopicEpisodeEntity { Episode = episodeEntity, Topic = topicEntity };
        _readWriteDbContext.TopicEpisodes.Add(topicEpisodeEntity);
    }

    private async Task CreatePersonTopicRelationshipIfNotExistsAsync(
        PersonEntity personEntity,
        TopicEntity topicEntity,
        CancellationToken cancellationToken)
    {
        PersonTopicEntity? existingPersonTopic = await _readWriteDbContext.PersonTopics
            .FirstOrDefaultAsync(
                pt => pt.Person.Id == personEntity.Id && pt.Topic.Id == topicEntity.Id,
                cancellationToken);

        if (existingPersonTopic is not null)
        {
            _logger.LogInformation(
                "Person '{Person}' is already associated with topic '{TopicName}'",
                personEntity.Name,
                topicEntity.Name);

            return;
        }

        var personTopicEntity = new PersonTopicEntity { Person = personEntity, Topic = topicEntity };
        _readWriteDbContext.PersonTopics.Add(personTopicEntity);
    }

    private async Task UpdateShowLastSyncedAtAsync(ShowEntity showEntity, CancellationToken cancellationToken)
    {
        showEntity.LastSyncedAt = DateTimeOffset.UtcNow;
        await _readWriteDbContext.SaveChangesAsync(cancellationToken);
    }
}
