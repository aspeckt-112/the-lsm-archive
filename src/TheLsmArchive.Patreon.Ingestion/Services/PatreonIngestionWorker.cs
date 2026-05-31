using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Parsers.Abstractions;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The background worker responsible for orchestrating Patreon RSS feed ingestion and processing.
/// </summary>
public sealed class PatreonIngestionWorker : BackgroundService
{
    private readonly ILogger<PatreonIngestionWorker> _logger;
    private readonly IPatreonRssParser _rssParser;
    private readonly IPatreonFeedProcessingService _feedProcessingService;
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
        IPatreonRssParser rssParser,
        IOptions<RssFeedSources> feedOptions,
        IOptions<PatreonIngestionOptions> ingestionOptions,
        IPatreonFeedProcessingService feedProcessingService)
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
            try
            {
                await RunIngestionCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Patreon ingestion cycle failed. The worker will retry after the configured delay.");
            }

            if (!await WaitForNextCycleAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task RunIngestionCycleAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting Patreon ingestion cycle. Next run in {IntervalMinutes} minutes.",
            _ingestionInterval.TotalMinutes);

        foreach (RssFeedSource source in _sources)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await ProcessSourceAsync(source, stoppingToken);
        }
    }

    private async Task ProcessSourceAsync(RssFeedSource source, CancellationToken stoppingToken)
    {
        try
        {
            await foreach (PatreonFeed feed in _rssParser.ParseFeedsAsync([source], stoppingToken))
            {
                await ProcessFeedAsync(feed, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to parse Patreon source '{SourceName}'. Continuing with the next source.",
                source.Name);
        }
    }

    private async Task ProcessFeedAsync(PatreonFeed feed, CancellationToken stoppingToken)
    {
        try
        {
            await _feedProcessingService.ProcessFeedAsync(feed, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process Patreon feed '{FeedTitle}'. Continuing with the next feed.",
                feed.Title);
        }
    }

    private async Task<bool> WaitForNextCycleAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            await Task.Delay(_ingestionInterval, stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
