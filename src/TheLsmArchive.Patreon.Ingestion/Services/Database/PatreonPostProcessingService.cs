using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Registry;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Helpers;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

/// <summary>
/// The service responsible for processing a single pending Patreon post through the AI summary
/// pipeline and persisting the resulting episode, persons, topics, and relationships.
/// </summary>
public sealed class PatreonPostProcessingService
{
    private readonly ILogger<PatreonPostProcessingService> _logger;
    private readonly LsmArchiveDbContext _dbContext;
    private readonly IAiSummaryService _aiSummaryService;
    private readonly ResiliencePipeline _aiSummaryPipeline;
    private readonly EpisodeService _episodeService;
    private readonly PersonService _personService;
    private readonly TopicService _topicService;
    private readonly RelationshipService _relationshipService;
    private readonly PatreonService _patreonService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonPostProcessingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="aiSummaryService">The AI summary service.</param>
    /// <param name="pipelineProvider">The resilience pipeline provider.</param>
    /// <param name="episodeService">The episode service.</param>
    /// <param name="personService">The person service.</param>
    /// <param name="topicService">The topic service.</param>
    /// <param name="relationshipService">The relationship service.</param>
    /// <param name="patreonService">The Patreon service.</param>
    public PatreonPostProcessingService(
        ILogger<PatreonPostProcessingService> logger,
        LsmArchiveDbContext dbContext,
        IAiSummaryService aiSummaryService,
        ResiliencePipelineProvider<string> pipelineProvider,
        EpisodeService episodeService,
        PersonService personService,
        TopicService topicService,
        RelationshipService relationshipService,
        PatreonService patreonService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _aiSummaryService = aiSummaryService;
        _aiSummaryPipeline = pipelineProvider.GetPipeline(Constants.Constants.AiSummaryPipelineName);
        _episodeService = episodeService;
        _personService = personService;
        _topicService = topicService;
        _relationshipService = relationshipService;
        _patreonService = patreonService;
    }

    /// <summary>
    /// Processes the given pending post: invokes the AI summary pipeline, then persists the
    /// resulting episode, persons, topics, and relationships in a single transaction.
    /// On failure, the error message is saved to the post for retry on the next ingestion cycle.
    /// </summary>
    /// <param name="showId">The ID of the show the post belongs to.</param>
    /// <param name="pendingPost">The pending post to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ProcessPostsForShow(int showId, PendingPost pendingPost, CancellationToken cancellationToken)
    {
        bool isRetry = pendingPost.ProcessingError is not null;

        if (isRetry)
        {
            _logger.LogInformation(
                "Retrying processing for post '{PostTitle}' (previous error: {Error})",
                pendingPost.Title,
                pendingPost.ProcessingError);
        }

        PatreonPostEntity postEntity = await _dbContext.PatreonPosts
            .AsNoTracking()
            .Include(p => p.Show)
            .FirstOrDefaultAsync(p => p.Id == pendingPost.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Patreon post with ID {pendingPost.Id} was not found.");

        ShowEntity showEntity = postEntity.Show;

        (List<string> knownPersons, List<string> knownTopics) =
            await GetKnownContextAsync(showId, cancellationToken);

        ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

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

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                int episodeId = await _episodeService.GetOrCreateAsync(postEntity, cancellationToken);

                // Resolve unique persons, deduplicated by normalized key to prevent duplicate
                // records when the AI returns case/spelling variants of the same name.
                var seenPersonKeys = new HashSet<string>();
                var personIds = new List<int>();

                foreach (string name in aiSummary.Hosts.Concat(aiSummary.Guests))
                {
                    string key = LookupKeyNormalizer.Normalize(name);

                    if (!seenPersonKeys.Add(key))
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing person '{Person}'", name);
                    personIds.Add(await _personService.GetOrCreateAsync(name, cancellationToken));
                }

                // Resolve unique topics using the same deduplication strategy.
                var seenTopicKeys = new HashSet<string>();
                var topicIds = new List<int>();

                foreach (string name in aiSummary.Topics)
                {
                    string key = LookupKeyNormalizer.Normalize(name);

                    if (!seenTopicKeys.Add(key))
                    {
                        continue;
                    }

                    _logger.LogInformation("Processing topic '{Topic}'", name);
                    topicIds.Add(await _topicService.GetOrCreateAsync(name, cancellationToken));
                }

                await _relationshipService.LinkPersonsToEpisodeAsync(personIds, episodeId, cancellationToken);
                await _relationshipService.LinkTopicsToEpisodeAsync(topicIds, episodeId, cancellationToken);
                await _relationshipService.LinkPersonsToTopicsAsync(personIds, topicIds, cancellationToken);

                // Flush the pending relationship rows and mark the post as processed.
                await _patreonService.MarkPostAsProcessedAsync(pendingPost.Id, episodeId, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully processed post '{PostTitle}'", pendingPost.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process post '{PostTitle}'. Error will be saved for retry.",
                    pendingPost.Title);

                await transaction.RollbackAsync(cancellationToken);

                // Clear stale tracked state from the failed attempt before saving the error.
                _dbContext.ChangeTracker.Clear();

                await _patreonService.SaveProcessingErrorAsync(pendingPost.Id, ex.Message, cancellationToken);

                throw;
            }
        });
    }

    private async Task<(List<string> Persons, List<string> Topics)> GetKnownContextAsync(
        int showId,
        CancellationToken cancellationToken)
    {
        // Top 50 persons by episode count (more frequent = more relevant for disambiguation).
        List<string> persons = await _dbContext.PersonEpisodes
            .Where(pe => pe.Episode.ShowId == showId)
            .GroupBy(pe => pe.Person.Name)
            .OrderByDescending(g => g.Count())
            .Take(50)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        // Top 150 topics by recency (ongoing discussions provide better AI context).
        List<string> topics = await _dbContext.TopicEpisodes
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
}
