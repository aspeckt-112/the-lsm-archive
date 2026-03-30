using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Models;

public class PatreonPostTests
{
    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        DateTimeOffset published = new(2024, 1, 15, 10, 0, 0, TimeSpan.FromHours(5));

        PatreonPost post = new(
            Id: 12345,
            Title: "Test Episode",
            Published: published,
            Summary: "<p>Test summary</p>",
            Link: "https://patreon.com/post/12345",
            AudioUrl: "https://audio.com/ep1.mp3");

        PatreonPostEntity entity = post.ToEntity(showId: 42);

        Assert.Equal(12345, entity.PatreonId);
        Assert.Equal("Test Episode", entity.Title);
        Assert.Equal(published.ToUniversalTime(), entity.Published);
        Assert.Equal("<p>Test summary</p>", entity.Summary);
        Assert.Equal("https://patreon.com/post/12345", entity.Link);
        Assert.Equal("https://audio.com/ep1.mp3", entity.AudioUrl);
        Assert.Equal(42, entity.ShowId);
    }

    [Fact]
    public void ToEntity_ConvertsPublishedToUtc()
    {
        DateTimeOffset published = new(2024, 1, 15, 15, 0, 0, TimeSpan.FromHours(5));

        PatreonPost post = new(1, "Title", published, "Summary", "https://link.com", "https://audio.com");

        PatreonPostEntity entity = post.ToEntity(1);

        Assert.Equal(TimeSpan.Zero, entity.Published.Offset);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero), entity.Published);
    }

    [Fact]
    public void ToEntity_PreservesUtcDate()
    {
        DateTimeOffset published = new(2024, 6, 20, 12, 0, 0, TimeSpan.Zero);

        PatreonPost post = new(1, "Title", published, "Summary", "https://link.com", "https://audio.com");

        PatreonPostEntity entity = post.ToEntity(1);

        Assert.Equal(published, entity.Published);
    }
}
