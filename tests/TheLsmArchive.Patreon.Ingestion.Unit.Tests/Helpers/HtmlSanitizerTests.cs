using TheLsmArchive.Patreon.Ingestion.Helpers;

namespace TheLsmArchive.Patreon.Ingestion.Unit.Tests.Helpers;

public class HtmlSanitizerTests
{
    [Theory]
    [InlineData("<p>Hello <strong>World</strong>!</p>", "Hello World!")]
    [InlineData("Line1<br>Line2", "Line1 Line2")]
    [InlineData("<div>First</div><div>Second</div>", "First Second")]
    [InlineData("Items:<ul><li>One</li><li>Two</li></ul>", "Items: One Two")]
    [InlineData("   Multiple   spaces   ", "Multiple spaces")]
    [InlineData("No tags or entities", "No tags or entities")]
    [InlineData("&lt;Encoded&gt; &amp; &quot;Entities&quot;", "<Encoded> & \"Entities\"")]
    [InlineData("&lt;p&gt;Encoded paragraph&lt;/p&gt;", "Encoded paragraph")]
    [InlineData("&#39;Quoted&#39;", "'Quoted'")]
    [InlineData("", "")]
    [InlineData(" \n\t ", "")]
    [InlineData(null, "")]
    public void StripHtml_ShouldRemoveTagsAndDecodeEntities(string? input, string expected)
    {
        // Act
        string result = HtmlSanitizer.StripHtml(input!);

        // Assert
        Equal(expected, result);
    }
}
