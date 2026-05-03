using System.Collections.Immutable;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TheLsmArchive.Domain.Models;
using TheLsmArchive.Domain.Services;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Parsers;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for orchestrating Patreon RSS feed ingestion and processing.
/// </summary>
public sealed class PatreonIngestionService : BackgroundService
{
    private readonly ILogger<PatreonIngestionService> _logger;
    private readonly PatreonRssParser _rssParser;
    private readonly ShowService _showService;
    private readonly PatreonService _patreonService;
    private readonly PatreonPostProcessingService _patreonPostProcessingService;
    private readonly List<RssFeedSource> _sources;
    private readonly TimeSpan _ingestionInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonIngestionService"/> class with the specified dependencies and configuration options.
    /// </summary>
    /// <param name="logger">The logger for logging informational messages and errors during the ingestion process.</param>
    /// <param name="rssParser">The service responsible for parsing Patreon RSS feeds into structured data models.</param>
    /// <param name="feedOptions">The configuration options containing the list of Patreon RSS feed sources to ingest.</param>
    /// <param name="ingestionOptions">The configuration options containing settings for the ingestion process, such as the refresh interval.</param>
    /// <param name="showService">The service for retrieving or creating show records in the database.</param>
    /// <param name="patreonService">The service for interacting with Patreon post records in the database, including ingesting new posts and retrieving pending posts for processing.</param>
    /// <param name="patreonPostProcessingService">The service responsible for processing individual Patreon posts, including extracting episode information and linking related entities.</param>
    public PatreonIngestionService(
        ILogger<PatreonIngestionService> logger,
        PatreonRssParser rssParser,
        IOptions<RssFeedSources> feedOptions,
        IOptions<PatreonIngestionOptions> ingestionOptions,
        ShowService showService,
        PatreonService patreonService,
        PatreonPostProcessingService patreonPostProcessingService)
    {
        _logger = logger;
        _rssParser = rssParser;
        _showService = showService;
        _patreonService = patreonService;
        _patreonPostProcessingService = patreonPostProcessingService;
        _sources = feedOptions.Value;
        _ingestionInterval = TimeSpan.FromMinutes(ingestionOptions.Value.RefreshIntervalInMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Starting Patreon ingestion cycle. Next run in {IntervalMinutes} minutes.",
                _ingestionInterval.TotalMinutes);

            await foreach (PatreonFeed feed in _rssParser.ParseFeedsAsync(_sources, stoppingToken))
            {
                Show show = await _showService.GetOrCreateAsync(feed.Title, stoppingToken);

                _logger.LogInformation("Ingesting feed '{FeedTitle}' for show ID {ShowId}", feed.Title, show.Id);

                await _patreonService.IngestFeed(show.Id, feed, stoppingToken);

                ImmutableList<PendingPost> postsToProcess = await _patreonService.GetPendingPosts(show.Id, stoppingToken);

                int retryCount = postsToProcess.Count(p => p.ProcessingError != null);

                if (retryCount > 0)
                {
                    _logger.LogInformation(
                        "Retrying {RetryCount} posts with previous processing errors",
                        retryCount);
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (PendingPost post in postsToProcess)
                {
                    try
                    {
                        await _patreonPostProcessingService.ProcessPostsForShow(show.Id, post, stoppingToken);
                        successCount++;
                    }
                    catch (Exception postEx)
                    {
                        _logger.LogError(postEx, "Failed to process post '{PostTitle}'", post.Title);
                        errorCount++;
                    }
                }

                _logger.LogInformation(
                    "Completed processing feed '{FeedTitle}': {SuccessCount} successful, {ErrorCount} failed",
                    feed.Title,
                    successCount,
                    errorCount);
            }

            try
            {
                await Task.Delay(_ingestionInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}