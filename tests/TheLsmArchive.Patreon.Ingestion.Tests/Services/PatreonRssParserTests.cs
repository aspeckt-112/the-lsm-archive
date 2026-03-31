using System.Net;
using System.Text;

using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Services;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class PatreonRssParserTests
{
    private static PatreonRssParser CreateParser(string responseXml)
    {
        MockHttpMessageHandler handler = new(responseXml);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://test.com") };
        return new PatreonRssParser(httpClient);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithValidRss_ReturnsParsedFeed()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>12345</guid>
                    <title>Episode 1</title>
                    <pubDate>Mon, 15 Jan 2024 10:00:00 GMT</pubDate>
                    <description>Episode description</description>
                    <link>https://patreon.com/post/12345</link>
                    <enclosure url="https://audio.com/ep1.mp3" />
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        List<PatreonFeed> feeds = [];
        await foreach (PatreonFeed feed in parser.ParseFeedsAsync(sources))
        {
            feeds.Add(feed);
        }

        Assert.Single(feeds);
        Assert.Equal("Test Show", feeds[0].Title);
        Assert.Single(feeds[0].Posts);
        Assert.Equal(12345, feeds[0].Posts[0].Id);
        Assert.Equal("Episode 1", feeds[0].Posts[0].Title);
        Assert.Equal("https://patreon.com/post/12345", feeds[0].Posts[0].Link);
        Assert.Equal("https://audio.com/ep1.mp3", feeds[0].Posts[0].AudioUrl);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithMultiplePosts_ReturnsAllPosts()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>1</guid>
                    <title>Episode 1</title>
                    <pubDate>Mon, 15 Jan 2024 10:00:00 GMT</pubDate>
                    <description>Desc 1</description>
                    <link>https://patreon.com/1</link>
                    <enclosure url="https://audio.com/1.mp3" />
                </item>
                <item>
                    <guid>2</guid>
                    <title>Episode 2</title>
                    <pubDate>Tue, 16 Jan 2024 10:00:00 GMT</pubDate>
                    <description>Desc 2</description>
                    <link>https://patreon.com/2</link>
                    <enclosure url="https://audio.com/2.mp3" />
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        List<PatreonFeed> feeds = [];
        await foreach (PatreonFeed feed in parser.ParseFeedsAsync(sources))
        {
            feeds.Add(feed);
        }

        Assert.Equal(2, feeds[0].Posts.Count);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithContentEncoded_PrefersContentOverDescription()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>1</guid>
                    <title>Episode 1</title>
                    <pubDate>Mon, 15 Jan 2024 10:00:00 GMT</pubDate>
                    <description>Short description</description>
                    <content:encoded><![CDATA[<p>Rich content description</p>]]></content:encoded>
                    <link>https://patreon.com/1</link>
                    <enclosure url="https://audio.com/1.mp3" />
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        List<PatreonFeed> feeds = [];
        await foreach (PatreonFeed feed in parser.ParseFeedsAsync(sources))
        {
            feeds.Add(feed);
        }

        Assert.Contains("Rich content", feeds[0].Posts[0].Summary);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithMissingEnclosure_ThrowsInvalidOperationException()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>1</guid>
                    <title>Episode 1</title>
                    <pubDate>Mon, 15 Jan 2024 10:00:00 GMT</pubDate>
                    <description>Desc</description>
                    <link>https://patreon.com/1</link>
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (PatreonFeed _ in parser.ParseFeedsAsync(sources)) { }
        });
    }

    [Fact]
    public async Task ParseFeedsAsync_WithMissingPubDate_ThrowsInvalidOperationException()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>1</guid>
                    <title>Episode 1</title>
                    <description>Desc</description>
                    <link>https://patreon.com/1</link>
                    <enclosure url="https://audio.com/1.mp3" />
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (PatreonFeed _ in parser.ParseFeedsAsync(sources)) { }
        });
    }

    [Fact]
    public async Task ParseFeedsAsync_WithMultipleSources_ReturnsMultipleFeeds()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>1</guid>
                    <title>Episode 1</title>
                    <pubDate>Mon, 15 Jan 2024 10:00:00 GMT</pubDate>
                    <description>Desc</description>
                    <link>https://patreon.com/1</link>
                    <enclosure url="https://audio.com/1.mp3" />
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources =
        [
            new() { Name = "Source 1", Url = "https://test.com/rss1" },
            new() { Name = "Source 2", Url = "https://test.com/rss2" }
        ];

        List<PatreonFeed> feeds = [];
        await foreach (PatreonFeed feed in parser.ParseFeedsAsync(sources))
        {
            feeds.Add(feed);
        }

        Assert.Equal(2, feeds.Count);
    }

    [Fact]
    public async Task ParseFeedsAsync_WithEmptyChannel_ReturnsEmptyPostList()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Empty Show</title>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        List<PatreonFeed> feeds = [];
        await foreach (PatreonFeed feed in parser.ParseFeedsAsync(sources))
        {
            feeds.Add(feed);
        }

        Assert.Single(feeds);
        Assert.Equal("Empty Show", feeds[0].Title);
        Assert.Empty(feeds[0].Posts);
    }

    [Fact]
    public async Task ParseFeedsAsync_ParsesPublishedDateCorrectly()
    {
        const string rssXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
            <channel>
                <title>Test Show</title>
                <item>
                    <guid>1</guid>
                    <title>Episode 1</title>
                    <pubDate>Sat, 20 Jul 2024 15:30:00 +00:00</pubDate>
                    <description>Desc</description>
                    <link>https://patreon.com/1</link>
                    <enclosure url="https://audio.com/1.mp3" />
                </item>
            </channel>
            </rss>
            """;

        PatreonRssParser parser = CreateParser(rssXml);
        List<RssFeedSource> sources = [new() { Name = "Test", Url = "https://test.com/rss" }];

        List<PatreonFeed> feeds = [];
        await foreach (PatreonFeed feed in parser.ParseFeedsAsync(sources))
        {
            feeds.Add(feed);
        }

        DateTimeOffset expected = new(2024, 7, 20, 15, 30, 0, TimeSpan.Zero);
        Assert.Equal(expected, feeds[0].Posts[0].Published);
    }

    /// <summary>
    /// A mock HTTP message handler that returns a predefined response for any request.
    /// </summary>
    private sealed class MockHttpMessageHandler(string responseContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/xml")
            });
        }
    }
}
