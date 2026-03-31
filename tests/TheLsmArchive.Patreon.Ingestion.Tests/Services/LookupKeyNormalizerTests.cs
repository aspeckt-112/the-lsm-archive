using TheLsmArchive.Patreon.Ingestion;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class LookupKeyNormalizerTests
{
    [Fact]
    public void NormalizeLookupKey_LowercasesAndStripsSpaces()
    {
        string result = LookupKeyNormalizer.Normalize("Colin Moriarty");

        Assert.Equal("colinmoriarty", result);
    }

    [Fact]
    public void NormalizeLookupKey_RemovesAccents()
    {
        string result = LookupKeyNormalizer.Normalize("José García");

        Assert.Equal("josegarcia", result);
    }

    [Fact]
    public void NormalizeLookupKey_HandlesUnicodeAccents()
    {
        string result = LookupKeyNormalizer.Normalize("Günter Müller");

        Assert.Equal("guntermuller", result);
    }

    [Fact]
    public void NormalizeLookupKey_StripsNonAlphanumericCharacters()
    {
        string result = LookupKeyNormalizer.Normalize("O'Brien-Smith");

        Assert.Equal("obriensmith", result);
    }

    [Fact]
    public void NormalizeLookupKey_PreservesDigits()
    {
        string result = LookupKeyNormalizer.Normalize("PlayStation 5");

        Assert.Equal("playstation5", result);
    }

    [Fact]
    public void NormalizeLookupKey_TrimsLeadingAndTrailingWhitespace()
    {
        string result = LookupKeyNormalizer.Normalize("  Colin  ");

        Assert.Equal("colin", result);
    }

    [Fact]
    public void NormalizeLookupKey_DifferentCasingProducesSameKey()
    {
        string upper = LookupKeyNormalizer.Normalize("COLIN MORIARTY");
        string lower = LookupKeyNormalizer.Normalize("colin moriarty");
        string mixed = LookupKeyNormalizer.Normalize("Colin Moriarty");

        Assert.Equal(upper, lower);
        Assert.Equal(lower, mixed);
    }

    [Fact]
    public void NormalizeLookupKey_WithPunctuation_StripsIt()
    {
        string result = LookupKeyNormalizer.Normalize("C# & .NET");

        Assert.Equal("cnet", result);
    }

    [Fact]
    public void NormalizeLookupKey_WithOnlySpecialCharacters_FallsBackToLowerInvariant()
    {
        // When all chars are stripped (no alphanumeric), falls back to trimmed.ToLowerInvariant()
        string result = LookupKeyNormalizer.Normalize("---");

        Assert.Equal("---", result);
    }

    [Fact]
    public void NormalizeLookupKey_WithEmoji_StripsNonAlphanumeric()
    {
        // Emoji are not alphanumeric and should be stripped
        string result = LookupKeyNormalizer.Normalize("Game 🎮 Time");

        Assert.Equal("gametime", result);
    }

    [Fact]
    public void NormalizeLookupKey_WithCedilla_RemovesAccent()
    {
        string result = LookupKeyNormalizer.Normalize("François");

        Assert.Equal("francois", result);
    }
}
