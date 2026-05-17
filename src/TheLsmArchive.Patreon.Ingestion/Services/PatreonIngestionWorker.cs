using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Parsers;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The background worker responsible for orchestrating Patreon RSS feed ingestion and processing.
/// </summary>
public sealed class PatreonIngestionWorker : BackgroundService
{
    private readonly ILogger<PatreonIngestionWorker> _logger;
    private readonly PatreonRssParser _rssParser;
    private readonly PatreonFeedProcessingService _feedProcessingService;
    private readonly List<RssFeedSource> _sources;
    private readonly TimeSpan _ingestionInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonIngestionWorker"/> class with the specified dependencies and configuration options.
    /// </summary>
    /// <param name="logger">The logger for logging informational messages and errors during the ingestion process.</param>
    /// <param name="rssParser">The service responsible for parsing Patreon RSS feeds into structured data models.</param>
    /// <param name="feedOptions">The configuration options containing the list of Patreon RSS feed sources to ingest.</param>
    /// <param name="ingestionOptions">The configuration options containing settings for the ingestion process, such as the refresh interval.</param>
    /// <param name="feedProcessingService">The service that handles feed ingestion and post processing for a parsed Patreon feed.</param>
    public PatreonIngestionWorker(
        ILogger<PatreonIngestionWorker> logger,
        PatreonRssParser rssParser,
        IOptions<RssFeedSources> feedOptions,
        IOptions<PatreonIngestionOptions> ingestionOptions,
        PatreonFeedProcessingService feedProcessingService)
    {
        _logger = logger;
        _rssParser = rssParser;
        _feedProcessingService = feedProcessingService;
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
                await _feedProcessingService.ProcessFeedAsync(feed, stoppingToken);
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


