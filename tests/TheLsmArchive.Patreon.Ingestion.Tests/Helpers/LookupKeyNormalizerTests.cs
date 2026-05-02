using TheLsmArchive.Patreon.Ingestion.Helpers;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Helpers;

public class LookupKeyNormalizerTests
{
    [Theory]
    [InlineData("Café", "cafe")]
    [InlineData("  Hello, World!  ", "helloworld")]
    [InlineData("Déjà vu!", "dejavu")]
    [InlineData("C# & .NET", "cnet")]
    [InlineData("123 ABC", "123abc")]
    [InlineData("Game 🎮 Time", "gametime")]
    [InlineData("!!!", "")]
    [InlineData("---", "")]
    [InlineData("   ", "")]
    public void Normalize_ShouldProduceCanonicalKeys(string input, string expected)
    {
        // Act
        string result = LookupKeyNormalizer.Normalize(input);

        // Assert
        Equal(expected, result);
    }

    [Fact]
    public void Normalize_ShouldStripCombiningMarksFromDecomposedUnicode()
    {
        // Arrange
        string value = "Cafe\u0301";

        // Act
        string result = LookupKeyNormalizer.Normalize(value);

        // Assert
        Equal("cafe", result);
    }
}
