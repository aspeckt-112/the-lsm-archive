using System.Globalization;
using System.Xml.Linq;

namespace TheLsmArchive.Patreon.Ingestion.Unit.Tests;

public class ExtensionsTests
{
    [Fact]
    public void Get_ShouldReturnTrimmedChildValue()
    {
        // Arrange
        var element = XElement.Parse("<item><title>  Sacred Symbols  </title></item>");

        // Act
        string result = element.Get("title");

        // Assert
        Equal("Sacred Symbols", result);
    }

    [Fact]
    public void Get_ShouldThrowWhenElementIsNull()
    {
        // Arrange
        XElement? element = null;

        // Act
        void Act()
        {
            _ = element.Get("title");
        }

        // Assert
        Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void Get_ShouldThrowWhenNameIsNull()
    {
        // Arrange
        var element = new XElement("item");
        XName? name = null;

        // Act
        void Act()
        {
            _ = element.Get(name!);
        }

        // Assert
        Throws<ArgumentNullException>(Act);
    }

    [Theory]
    [InlineData("<item />")]
    [InlineData("<item><title></title></item>")]
    [InlineData("<item><title>   </title></item>")]
    public void Get_ShouldThrowWhenChildValueIsMissingOrEmpty(string xml)
    {
        // Arrange
        var element = XElement.Parse(xml);

        // Act
        void Act()
        {
            _ = element.Get("title");
        }

        // Assert
        InvalidOperationException exception = Throws<InvalidOperationException>(Act);
        Equal("Missing or empty value for element 'title' under 'item'.", exception.Message);
    }

    [Theory]
    [InlineData("Fri, 17 Apr 2026 10:30:45 -04:00", "2026-04-17T10:30:45-04:00")]
    [InlineData("Fri, 17 Apr 2026 14:30:45 GMT", "2026-04-17T14:30:45+00:00")]
    [InlineData("17 Apr 2026 10:30:45 -04:00", "2026-04-17T10:30:45-04:00")]
    [InlineData("  Fri, 17 Apr 2026 10:30:45 -04:00  ", "2026-04-17T10:30:45-04:00")]
    public void GetDateTimeOffset_ShouldParseSupportedFormats(string value, string expected)
    {
        // Arrange
        var element = new XElement("pubDate", value);
        var expectedValue = DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture);

        // Act
        DateTimeOffset? result = element.GetDateTimeOffset();

        // Assert
        NotNull(result);
        Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Not a date")]
    public void GetDateTimeOffset_ShouldReturnNullForMissingEmptyOrInvalidValues(string? value)
    {
        // Arrange
        XElement? element = value is null ? null : new XElement("pubDate", value);

        // Act
        DateTimeOffset? result = element.GetDateTimeOffset();

        // Assert
        Null(result);
    }
}
