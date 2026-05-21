using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Parsers.Abstractions;
using TheLsmArchive.Patreon.Ingestion.Services;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class PatreonIngestionWorkerTests
{
    [Fact]
    public async Task StartAsync_WhenFeedProcessingThrows_ContinuesToNextFeed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PatreonFeed firstFeed = CreateFeed("Feed A");
        PatreonFeed secondFeed = CreateFeed("Feed B");
        ConcurrentQueue<string> processedFeedTitles = [];
        TaskCompletionSource<bool> secondFeedProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<IPatreonRssParser> rssParser = new(MockBehavior.Strict);
        rssParser
            .Setup(parser => parser.ParseFeedsAsync(It.IsAny<IEnumerable<RssFeedSource>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<RssFeedSource> _, CancellationToken ct) => YieldFeedsAsync([firstFeed, secondFeed], ct));

        Mock<IPatreonFeedProcessingService> feedProcessingService = new(MockBehavior.Strict);
        feedProcessingService
            .Setup(service => service.ProcessFeedAsync(It.IsAny<PatreonFeed>(), It.IsAny<CancellationToken>()))
            .Returns<PatreonFeed, CancellationToken>((feed, _) =>
            {
                processedFeedTitles.Enqueue(feed.Title);

                if (feed.Title == firstFeed.Title)
                {
                    return Task.FromException(new InvalidOperationException("Processing failed."));
                }

                secondFeedProcessed.TrySetResult(true);
                return Task.CompletedTask;
            });

        PatreonIngestionWorker worker = CreateWorker(
            rssParser.Object,
            feedProcessingService.Object,
            refreshIntervalInMinutes: 10);

        await worker.StartAsync(cancellationToken);
        await secondFeedProcessed.Task.WaitAsync(cancellationToken);
        await worker.StopAsync(cancellationToken);

        Equal([firstFeed.Title, secondFeed.Title], processedFeedTitles.ToArray());
        rssParser.Verify(
            parser => parser.ParseFeedsAsync(It.IsAny<IEnumerable<RssFeedSource>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenSourceParsingThrows_ContinuesToNextSourceInSameCycle()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RssFeedSource firstSource = CreateSource("Source A", "https://patreon.com/rss/a");
        RssFeedSource secondSource = CreateSource("Source B", "https://patreon.com/rss/b");
        PatreonFeed secondFeed = CreateFeed("Feed B");
        ConcurrentQueue<string> parsedSourceNames = [];
        ConcurrentQueue<string> processedFeedTitles = [];
        TaskCompletionSource<bool> secondSourceProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<IPatreonRssParser> rssParser = new(MockBehavior.Strict);
        rssParser
            .Setup(parser => parser.ParseFeedsAsync(It.IsAny<IEnumerable<RssFeedSource>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<RssFeedSource> sources, CancellationToken ct) =>
            {
                RssFeedSource source = Single(sources);
                parsedSourceNames.Enqueue(source.Name);

                return source.Name switch
                {
                    "Source A" => ThrowParsingFailureAsync(ct),
                    "Source B" => YieldFeedsAsync([secondFeed], ct),
                    _ => throw new InvalidOperationException($"Unexpected source '{source.Name}'.")
                };
            });

        Mock<IPatreonFeedProcessingService> feedProcessingService = new(MockBehavior.Strict);
        feedProcessingService
            .Setup(service => service.ProcessFeedAsync(It.IsAny<PatreonFeed>(), It.IsAny<CancellationToken>()))
            .Returns<PatreonFeed, CancellationToken>((feed, _) =>
            {
                processedFeedTitles.Enqueue(feed.Title);
                secondSourceProcessed.TrySetResult(true);
                return Task.CompletedTask;
            });

        PatreonIngestionWorker worker = CreateWorker(
            rssParser.Object,
            feedProcessingService.Object,
            refreshIntervalInMinutes: 10,
            [firstSource, secondSource]);

        await worker.StartAsync(cancellationToken);
        await secondSourceProcessed.Task.WaitAsync(cancellationToken);
        await worker.StopAsync(cancellationToken);

        Equal([firstSource.Name, secondSource.Name], parsedSourceNames.ToArray());
        Equal([secondFeed.Title], processedFeedTitles.ToArray());
        feedProcessingService.Verify(
            service => service.ProcessFeedAsync(It.IsAny<PatreonFeed>(), It.IsAny<CancellationToken>()),
            Times.Once);
        rssParser.Verify(
            parser => parser.ParseFeedsAsync(It.IsAny<IEnumerable<RssFeedSource>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_WhenSourceParsingThrowsAfterYieldingFeed_ContinuesToNextSourceInSameCycle()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        RssFeedSource firstSource = CreateSource("Source A", "https://patreon.com/rss/a");
        RssFeedSource secondSource = CreateSource("Source B", "https://patreon.com/rss/b");
        PatreonFeed firstFeed = CreateFeed("Feed A");
        PatreonFeed secondFeed = CreateFeed("Feed B");
        ConcurrentQueue<string> parsedSourceNames = [];
        ConcurrentQueue<string> processedFeedTitles = [];
        TaskCompletionSource<bool> secondSourceProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<IPatreonRssParser> rssParser = new(MockBehavior.Strict);
        rssParser
            .Setup(parser => parser.ParseFeedsAsync(It.IsAny<IEnumerable<RssFeedSource>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<RssFeedSource> sources, CancellationToken ct) =>
            {
                RssFeedSource source = Single(sources);
                parsedSourceNames.Enqueue(source.Name);

                return source.Name switch
                {
                    "Source A" => YieldThenThrowAsync(firstFeed, ct),
                    "Source B" => YieldFeedsAsync([secondFeed], ct),
                    _ => throw new InvalidOperationException($"Unexpected source '{source.Name}'.")
                };
            });

        Mock<IPatreonFeedProcessingService> feedProcessingService = new(MockBehavior.Strict);
        feedProcessingService
            .Setup(service => service.ProcessFeedAsync(It.IsAny<PatreonFeed>(), It.IsAny<CancellationToken>()))
            .Returns<PatreonFeed, CancellationToken>((feed, _) =>
            {
                processedFeedTitles.Enqueue(feed.Title);

                if (feed.Title == secondFeed.Title)
                {
                    secondSourceProcessed.TrySetResult(true);
                }

                return Task.CompletedTask;
            });

        PatreonIngestionWorker worker = CreateWorker(
            rssParser.Object,
            feedProcessingService.Object,
            refreshIntervalInMinutes: 10,
            [firstSource, secondSource]);

        await worker.StartAsync(cancellationToken);
        await secondSourceProcessed.Task.WaitAsync(cancellationToken);
        await worker.StopAsync(cancellationToken);

        Equal([firstSource.Name, secondSource.Name], parsedSourceNames.ToArray());
        Equal([firstFeed.Title, secondFeed.Title], processedFeedTitles.ToArray());
        feedProcessingService.Verify(
            service => service.ProcessFeedAsync(It.IsAny<PatreonFeed>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        rssParser.Verify(
            parser => parser.ParseFeedsAsync(It.IsAny<IEnumerable<RssFeedSource>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static PatreonIngestionWorker CreateWorker(
        IPatreonRssParser rssParser,
        IPatreonFeedProcessingService feedProcessingService,
        int refreshIntervalInMinutes,
        IEnumerable<RssFeedSource>? sources = null)
    {
        return new PatreonIngestionWorker(
            NullLogger<PatreonIngestionWorker>.Instance,
            rssParser,
            new OptionsWrapper<RssFeedSources>([..(sources ?? [CreateSource("Test Feed", "https://patreon.com/rss/test")])]),
            new OptionsWrapper<PatreonIngestionOptions>(new PatreonIngestionOptions
            {
                RefreshIntervalInMinutes = refreshIntervalInMinutes
            }),
            feedProcessingService);
    }

    private static RssFeedSource CreateSource(string name, string url) =>
        new()
        {
            Name = name,
            Url = url
        };

    private static PatreonFeed CreateFeed(string title) =>
        new(title, [new PatreonPost(1, $"{title} Post", DateTimeOffset.UnixEpoch, "Summary", "https://example.com/post", "https://example.com/audio")]);

    private static async IAsyncEnumerable<PatreonFeed> YieldFeedsAsync(
        IEnumerable<PatreonFeed> feeds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (PatreonFeed feed in feeds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return feed;
            await Task.Yield();
        }
    }

    private static IAsyncEnumerable<PatreonFeed> ThrowParsingFailureAsync(CancellationToken cancellationToken = default) =>
        new ThrowingFeedEnumerable(cancellationToken);

    private static IAsyncEnumerable<PatreonFeed> YieldThenThrowAsync(
        PatreonFeed feed,
        CancellationToken cancellationToken = default) =>
        new YieldThenThrowFeedEnumerable(feed, cancellationToken);

    private sealed class ThrowingFeedEnumerable(CancellationToken sourceCancellationToken)
        : IAsyncEnumerable<PatreonFeed>, IAsyncEnumerator<PatreonFeed>
    {
        public PatreonFeed Current => default!;

        public IAsyncEnumerator<PatreonFeed> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new ThrowingFeedEnumerable(cancellationToken);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync()
        {
            sourceCancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromException<bool>(new InvalidOperationException("Parsing failed."));
        }
    }

    private sealed class YieldThenThrowFeedEnumerable(
        PatreonFeed feed,
        CancellationToken sourceCancellationToken)
        : IAsyncEnumerable<PatreonFeed>, IAsyncEnumerator<PatreonFeed>
    {
        private int _moveNextCount;

        public PatreonFeed Current { get; private set; } = default!;

        public IAsyncEnumerator<PatreonFeed> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new YieldThenThrowFeedEnumerable(feed, cancellationToken);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync()
        {
            sourceCancellationToken.ThrowIfCancellationRequested();

            return _moveNextCount++ switch
            {
                0 => YieldCurrentAsync(),
                1 => ValueTask.FromException<bool>(new InvalidOperationException("Parsing failed.")),
                _ => ValueTask.FromResult(false)
            };

            ValueTask<bool> YieldCurrentAsync()
            {
                Current = feed;
                return ValueTask.FromResult(true);
            }
        }
    }
}




