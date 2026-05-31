using System.Net;
using System.Text;
using System.Xml;

using Moq;
using Moq.Protected;

using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Parsers;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Parsers;

public class PatreonRssParserTests
{
    private const string ContentNamespace = "http://purl.org/rss/1.0/modules/content/";

    [Fact]
    public async Task ParseFeedsAsync_WithValidFeed_ReturnsParsedFeed()
    {
        // Arrange
        string rss = BuildRss(items: BuildItem());
        PatreonRssParser parser = CreateParser(rss);

        // Act
        CancellationToken ct = TestContext.Current.CancellationToken;
        List<PatreonFeed> feeds = await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct);

        // Assert
        Single(feeds);
        PatreonFeed feed = feeds[0];
        Equal("Test Channel", feed.Title);
        Single(feed.Posts);

        PatreonPost post = feed.Posts[0];
        Equal(99999, post.Id);
        Equal("Episode Title", post.Title);
        Equal(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), post.Published);
        Equal("Episode summary text", post.Summary);
        Equal("https://www.patreon.com/posts/99999", post.Link);
        Equal("https://example.com/audio.mp3", post.AudioUrl);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithMultipleSources_ReturnsFeedPerSource()
    {
        // Arrange
        string[] responses = [BuildRss(channelTitle: "Feed A"), BuildRss(channelTitle: "Feed B")];
        int callCount = 0;

        Mock<HttpMessageHandler> mockHandler = new(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responses[callCount++], Encoding.UTF8, "application/rss+xml")
            });

        PatreonRssParser parser = new(new HttpClient(mockHandler.Object));
        RssFeedSource[] sources =
        [
            new() { Name = "A", Url = "https://patreon.com/rss/a" },
            new() { Name = "B", Url = "https://patreon.com/rss/b" }
        ];

        // Act
        CancellationToken ct = TestContext.Current.CancellationToken;
        List<PatreonFeed> feeds = await CollectAsync(parser.ParseFeedsAsync(sources, ct), ct);

        // Assert
        Equal(2, feeds.Count);
        Equal("Feed A", feeds[0].Title);
        Equal("Feed B", feeds[1].Title);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithMultipleItems_ReturnsAllPosts()
    {
        // Arrange
        string items = BuildItem(id: "1", title: "Post One") + BuildItem(id: "2", title: "Post Two");
        PatreonRssParser parser = CreateParser(BuildRss(items: items));

        // Act
        CancellationToken ct = TestContext.Current.CancellationToken;
        List<PatreonFeed> feeds = await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct);

        // Assert
        Equal(2, feeds[0].Posts.Count);
        Equal("Post One", feeds[0].Posts[0].Title);
        Equal("Post Two", feeds[0].Posts[1].Title);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithNoItems_ReturnsEmptyPostsList()
    {
        // Arrange
        PatreonRssParser parser = CreateParser(BuildRss());

        // Act
        CancellationToken ct = TestContext.Current.CancellationToken;
        List<PatreonFeed> feeds = await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct);

        // Assert
        Single(feeds);
        Empty(feeds[0].Posts);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenContentEncodedAbsent_FallsBackToDescription()
    {
        // Arrange
        string item = BuildItem(contentEncoded: null, description: "Fallback description");
        PatreonRssParser parser = CreateParser(BuildRss(items: item));

        // Act
        CancellationToken ct = TestContext.Current.CancellationToken;
        List<PatreonFeed> feeds = await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct);

        // Assert
        Equal("Fallback description", feeds[0].Posts[0].Summary);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenHttpRequestFails_Throws()
    {
        // Arrange
        PatreonRssParser parser = CreateParser("<rss/>", HttpStatusCode.InternalServerError);

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        await ThrowsAsync<HttpRequestException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenXmlIsEmpty_Throws()
    {
        // Arrange
        PatreonRssParser parser = CreateParser(string.Empty);

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        await ThrowsAsync<XmlException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenChannelElementMissing_Throws()
    {
        // Arrange
        PatreonRssParser parser = CreateParser("<rss version=\"2.0\"><notchannel/></rss>");

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        InvalidOperationException ex = await ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
        Contains("Not an RSS 2.0", ex.Message);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenPubDateElementMissing_Throws()
    {
        // Arrange
        string item = $"""
                       <item>
                         <guid>1</guid>
                         <title>No Date</title>
                         <content:encoded>Summary</content:encoded>
                         <link>https://patreon.com/posts/1</link>
                         <enclosure url="https://example.com/audio.mp3" type="audio/mpeg" length="1"/>
                       </item>
                       """;
        PatreonRssParser parser = CreateParser(BuildRss(items: item));

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        InvalidOperationException ex = await ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
        Contains("Missing publication date element", ex.Message);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenPubDateInvalid_Throws()
    {
        // Arrange
        PatreonRssParser parser = CreateParser(BuildRss(items: BuildItem(pubDate: "not-a-date")));

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        InvalidOperationException ex = await ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
        Contains("Invalid publication date format", ex.Message);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenEnclosureElementMissing_Throws()
    {
        // Arrange
        string item = $"""
                       <item>
                         <guid>1</guid>
                         <title>No Enclosure</title>
                         <pubDate>Mon, 01 Jan 2024 10:00:00 GMT</pubDate>
                         <content:encoded>Summary</content:encoded>
                         <link>https://patreon.com/posts/1</link>
                       </item>
                       """;
        PatreonRssParser parser = CreateParser(BuildRss(items: item));

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        InvalidOperationException ex = await ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
        Contains("Missing enclosure element", ex.Message);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenAudioUrlMissing_Throws()
    {
        // Arrange
        PatreonRssParser parser = CreateParser(BuildRss(items: BuildItem(audioUrl: null)));

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        InvalidOperationException ex = await ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
        Contains("Missing audio URL", ex.Message);
    }

    [Fact]
    public async Task ParseFeedsAsync_WhenSummaryIsEmpty_Throws()
    {
        // Arrange – content:encoded has whitespace; it is read via ?.Value (not Get),
        // so the IsNullOrWhiteSpace guard in ParsePosts is the first to fire.
        PatreonRssParser parser = CreateParser(BuildRss(items: BuildItem(contentEncoded: "   ")));

        // Act & Assert
        CancellationToken ct = TestContext.Current.CancellationToken;
        InvalidOperationException ex = await ThrowsAsync<InvalidOperationException>(
            async () => await CollectAsync(parser.ParseFeedsAsync([MakeSource()], ct), ct));
        Contains("Missing episode summary", ex.Message);
    }

    private static PatreonRssParser CreateParser(string responseXml, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Mock<HttpMessageHandler> mockHandler = new(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseXml, Encoding.UTF8, "application/rss+xml")
            });

        return new PatreonRssParser(new HttpClient(mockHandler.Object));
    }

    private static RssFeedSource MakeSource() =>
        new() { Name = "Test Feed", Url = "https://www.patreon.com/rss/test" };

    private static string BuildRss(string channelTitle = "Test Channel", string items = "") =>
        $"""
         <?xml version="1.0" encoding="UTF-8"?>
         <rss version="2.0" xmlns:content="{ContentNamespace}">
           <channel>
             <title>{channelTitle}</title>
             {items}
           </channel>
         </rss>
         """;

    private static string BuildItem(
        string id = "99999",
        string title = "Episode Title",
        string pubDate = "Mon, 01 Jan 2024 10:00:00 GMT",
        string? contentEncoded = "Episode summary text",
        string? description = null,
        string link = "https://www.patreon.com/posts/99999",
        string? audioUrl = "https://example.com/audio.mp3")
    {
        string content = contentEncoded is not null
            ? $"<content:encoded><![CDATA[{contentEncoded}]]></content:encoded>"
            : string.Empty;

        string desc = description is not null
            ? $"<description>{description}</description>"
            : string.Empty;

        string enclosure = audioUrl is not null
            ? $"""<enclosure url="{audioUrl}" type="audio/mpeg" length="1234"/>"""
            : """<enclosure type="audio/mpeg" length="1234"/>""";

        return $"""
                <item>
                  <guid>{id}</guid>
                  <title>{title}</title>
                  <pubDate>{pubDate}</pubDate>
                  {content}
                  {desc}
                  <link>{link}</link>
                  {enclosure}
                </item>
                """;
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        List<T> result = [];

        await foreach (T item in source.WithCancellation(cancellationToken))
        {
            result.Add(item);
        }

        return result;
    }
}
