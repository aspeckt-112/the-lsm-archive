namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class HtmlSanitizerTests
{
    [Fact]
    public void StripHtml_WithPlainText_ReturnsUnchanged()
    {
        string result = HtmlSanitizer.StripHtml("Hello world");

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void StripHtml_WithHtmlTags_RemovesThem()
    {
        string result = HtmlSanitizer.StripHtml("<p>Hello <b>world</b></p>");

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void StripHtml_WithNestedTags_RemovesAll()
    {
        string result = HtmlSanitizer.StripHtml("<div><span class=\"highlight\">Important</span> text</div>");

        Assert.Equal("Important text", result);
    }

    [Fact]
    public void StripHtml_WithHtmlEntities_DecodesThemn()
    {
        string result = HtmlSanitizer.StripHtml("&amp; &lt; &gt; &quot;");

        Assert.Equal("& < > \"", result);
    }

    [Fact]
    public void StripHtml_WithExcessiveWhitespace_NormalizesToSingleSpaces()
    {
        string result = HtmlSanitizer.StripHtml("Hello    world\n\t  test");

        Assert.Equal("Hello world test", result);
    }

    [Fact]
    public void StripHtml_WithNull_ReturnsEmpty()
    {
        string result = HtmlSanitizer.StripHtml(null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void StripHtml_WithEmpty_ReturnsEmpty()
    {
        string result = HtmlSanitizer.StripHtml("");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void StripHtml_WithWhitespaceOnly_ReturnsEmpty()
    {
        string result = HtmlSanitizer.StripHtml("   ");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void StripHtml_WithLineBreakTags_RemovesThem()
    {
        string result = HtmlSanitizer.StripHtml("Line one<br/>Line two<br>Line three");

        // Tags are stripped without inserting spaces; adjacent text merges
        Assert.Equal("Line oneLine twoLine three", result);
    }

    [Fact]
    public void StripHtml_WithComplexHtml_ExtractsText()
    {
        string html = """
            <div class="post-content">
                <h1>Episode 150</h1>
                <p>Colin &amp; Chris discuss the <em>latest</em> PlayStation news.</p>
                <ul>
                    <li>PS5 Pro</li>
                    <li>Game Pass</li>
                </ul>
            </div>
            """;

        string result = HtmlSanitizer.StripHtml(html);

        Assert.Contains("Episode 150", result);
        Assert.Contains("Colin & Chris", result);
        Assert.Contains("latest", result);
        Assert.Contains("PS5 Pro", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Fact]
    public void StripHtml_WithSelfClosingTags_RemovesThem()
    {
        string result = HtmlSanitizer.StripHtml("Image: <img src=\"photo.jpg\" /> here");

        Assert.Equal("Image: here", result);
    }

    [Fact]
    public void StripHtml_WithNumericEntities_DecodesThemn()
    {
        string result = HtmlSanitizer.StripHtml("&#39;hello&#39;");

        Assert.Equal("'hello'", result);
    }
}
