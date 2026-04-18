using System.Xml.Linq;

using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;

namespace TheLsmArchive.Patreon.Ingestion.Services.RSS;

/// <summary>
/// Service responsible for fetching and parsing Patreon RSS feeds.
/// </summary>
public sealed class PatreonRssParser
{
    /// <summary>
    /// The XML namespace for content modules in RSS feeds.
    /// </summary>
    private const string ContentNamespace = "http://purl.org/rss/1.0/modules/content/";

    /// <summary>
    /// The standard RSS element names.
    /// </summary>
    private const string Channel = "channel";

    /// <summary>
    /// The RSS item element name.
    /// </summary>
    private const string Item = "item";

    /// <summary>
    /// The RSS title element name.
    /// </summary>
    private const string Title = "title";

    /// <summary>
    /// The RSS guid element name.
    /// </summary>
    private const string Id = "guid";

    /// <summary>
    /// The RSS publication date element name.
    /// </summary>
    private const string PubDate = "pubDate";

    /// <summary>
    /// The RSS content:encoded element name.
    /// </summary>
    private const string Encoded = "encoded";

    /// <summary>
    /// The RSS description element name.
    /// </summary>
    private const string Description = "description";

    /// <summary>
    /// The RSS link element name.
    /// </summary>
    private const string Link = "link";

    /// <summary>
    /// The RSS enclosure element name.
    /// </summary>
    private const string Enclosure = "enclosure";

    /// <summary>
    /// The RSS url attribute name.
    /// </summary>
    private const string Url = "url";

    private readonly HttpClient _httpClient;

    public PatreonRssParser(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetches and parses all configured RSS feed sources.
    /// </summary>
    /// <param name="sources">The RSS feed sources to parse.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of parsed feed results.</returns>
    public async IAsyncEnumerable<PatreonFeed> ParseFeedsAsync(
        IEnumerable<RssFeedSource> sources,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        foreach (RssFeedSource source in sources)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(source.Url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            XDocument document = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, cancellationToken);

            PatreonFeed result = ParseDocument(document);
            yield return result;
        }
    }

    /// <summary>
    /// Parses an XDocument into a PatreonFeedParseResult.
    /// </summary>
    /// <param name="document">The XML document to parse.</param>
    /// <returns>The parsed feed result.</returns>
    private static PatreonFeed ParseDocument(XDocument document)
    {
        XElement root = document.Root ?? throw new InvalidOperationException("Invalid RSS feed: Missing root element");

        XElement channel = root.Element(Channel) ??
                           throw new InvalidOperationException("Not an RSS 2.0 feed.");

        var postElements = channel.Elements(Item).ToList();

        string title = channel.Get(Title);

        List<PatreonPost> posts = ParsePosts(postElements);

        return new PatreonFeed(title, posts);
    }

    /// <summary>
    /// Parses a list of XML elements into PatreonPost objects.
    /// </summary>
    /// <param name="postElements">The post XML elements to parse.</param>
    /// <returns>A list of parsed posts.</returns>
    private static List<PatreonPost> ParsePosts(List<XElement> postElements)
    {
        List<PatreonPost> posts = [];
        XNamespace contentNs = ContentNamespace;

        foreach (XElement post in postElements)
        {
            int postId = Convert.ToInt32(post.Get(Id));
            string postTitle = post.Get(Title);

            XElement pubDateElement = post.Element(PubDate) ??
                                      throw new InvalidOperationException("Missing publication date element.");

            DateTimeOffset publishedDate = pubDateElement.GetDateTimeOffset() ??
                                           throw new InvalidOperationException("Invalid publication date format.");

            string episodeSummary = post.Element(contentNs + Encoded)?.Value ??
                                    post.Get(Description);

            if (string.IsNullOrWhiteSpace(episodeSummary))
            {
                throw new InvalidOperationException("Missing episode summary.");
            }

            string postLink = post.Get(Link);

            XElement enclosure = post.Element(Enclosure) ??
                                 throw new InvalidOperationException("Missing enclosure element.");

            string? audioUrl = (string?)enclosure.Attribute(Url);

            if (string.IsNullOrWhiteSpace(audioUrl))
            {
                throw new InvalidOperationException("Missing audio URL in enclosure.");
            }

            posts.Add(new PatreonPost(postId, postTitle, publishedDate, episodeSummary, postLink, audioUrl));
        }

        return posts;
    }
}
