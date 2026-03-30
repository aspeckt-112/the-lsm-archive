using System.Xml.Linq;

namespace TheLsmArchive.Patreon.Ingestion.Tests;

public class ExtensionsTests
{
    [Fact]
    public void Get_WithValidChildElement_ReturnsTrimmedValue()
    {
        XElement element = new("root", new XElement("child", "  test value  "));

        string result = element.Get("child");

        Assert.Equal("test value", result);
    }

    [Fact]
    public void Get_WithNullElement_ThrowsArgumentNullException()
    {
        XElement? element = null;

        Assert.Throws<ArgumentNullException>(() => element.Get("child"));
    }

    [Fact]
    public void Get_WithNullName_ThrowsArgumentNullException()
    {
        XElement element = new("root", new XElement("child", "value"));

        Assert.Throws<ArgumentNullException>(() => element.Get(null!));
    }

    [Fact]
    public void Get_WithMissingChild_ThrowsInvalidOperationException()
    {
        XElement element = new("root");

        Assert.Throws<InvalidOperationException>(() => element.Get("missing"));
    }

    [Fact]
    public void Get_WithEmptyChild_ThrowsInvalidOperationException()
    {
        XElement element = new("root", new XElement("child", "   "));

        Assert.Throws<InvalidOperationException>(() => element.Get("child"));
    }

    [Fact]
    public void GetDateTimeOffset_WithStandardRssDate_ReturnsParsedDate()
    {
        XElement element = new("pubDate", "Mon, 15 Jan 2024 10:00:00 +00:00");

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.NotNull(result);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero), result.Value);
    }

    [Fact]
    public void GetDateTimeOffset_WithGmtFormat_ReturnsParsedDate()
    {
        XElement element = new("pubDate", "Mon, 15 Jan 2024 10:00:00 GMT");

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.NotNull(result);
        Assert.Equal(2024, result.Value.Year);
        Assert.Equal(1, result.Value.Month);
        Assert.Equal(15, result.Value.Day);
    }

    [Fact]
    public void GetDateTimeOffset_WithPositiveOffset_ReturnsParsedDate()
    {
        XElement element = new("pubDate", "Wed, 20 Mar 2024 14:30:00 +05:30");

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.NotNull(result);
        Assert.Equal(new DateTimeOffset(2024, 3, 20, 14, 30, 0, TimeSpan.FromHours(5.5)), result.Value);
    }

    [Fact]
    public void GetDateTimeOffset_WithNullElement_ReturnsNull()
    {
        XElement? element = null;

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.Null(result);
    }

    [Fact]
    public void GetDateTimeOffset_WithEmptyValue_ReturnsNull()
    {
        XElement element = new("pubDate", "   ");

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.Null(result);
    }

    [Fact]
    public void GetDateTimeOffset_WithInvalidFormat_ReturnsNull()
    {
        XElement element = new("pubDate", "not-a-date");

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.Null(result);
    }

    [Fact]
    public void GetDateTimeOffset_WithShortDateFormat_ReturnsParsedDate()
    {
        // Third format: "dd MMM yyyy HH':'mm':'ss zzz"
        XElement element = new("pubDate", "15 Jan 2024 10:00:00 +00:00");

        DateTimeOffset? result = element.GetDateTimeOffset();

        Assert.NotNull(result);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero), result.Value);
    }
}
