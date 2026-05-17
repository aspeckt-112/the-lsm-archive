using System.Text.Json;

using Google.GenAI.Types;

using TheLsmArchive.Patreon.Ingestion.Services;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class GeminiMetadataExtractionServiceTests
{
    [Fact]
    public void ParseExtractedMetadata_ShouldThrowWhenCandidatesAreMissing()
    {
        // Arrange
        var response = new GenerateContentResponse { Candidates = null };

        // Act
        void Act()
        {
            _ = GeminiMetadataExtractionService.ParseExtractedMetadata(response);
        }

        // Assert
        InvalidDataException exception = Throws<InvalidDataException>(Act);
        Equal("Received empty candidates from Gemini.", exception.Message);
    }

    [Fact]
    public void ParseExtractedMetadata_ShouldThrowWhenContentPartsAreMissing()
    {
        // Arrange
        var response = new GenerateContentResponse
        {
            Candidates =
            [
                new Candidate
                {
                    Content = new Content { Parts = null }
                }
            ]
        };

        // Act
        void Act()
        {
            _ = GeminiMetadataExtractionService.ParseExtractedMetadata(response);
        }

        // Assert
        InvalidDataException exception = Throws<InvalidDataException>(Act);
        Equal("Received empty content parts from Gemini.", exception.Message);
    }

    [Fact]
    public void ParseExtractedMetadata_ShouldThrowWhenJsonIsInvalid()
    {
        // Arrange
        GenerateContentResponse response = CreateResponse("{");

        // Act
        void Act()
        {
            _ = GeminiMetadataExtractionService.ParseExtractedMetadata(response);
        }

        // Assert
        InvalidDataException exception = Throws<InvalidDataException>(Act);
        Equal("Failed to deserialize Gemini response.", exception.Message);
        IsType<JsonException>(exception.InnerException);
    }

    private static readonly string[] expected = new[] { "Colin Moriarty", "Chris Ray Gun" };
    private static readonly string[] expectedArray = new[] { "Dustin Furman" };
    private static readonly string[] expectedArray0 = new[] { "Game Pass", "PlayStation 5" };

    [Fact]
    public void ParseExtractedMetadata_ShouldTrimFilterAndDeduplicateValues()
    {
        // Arrange
        GenerateContentResponse response = CreateResponse(
            """
            {
              "hosts": [" Colin Moriarty ", null, "colin moriarty", "Chris Ray Gun"],
              "guests": [" Dustin Furman ", "COLIN MORIARTY", ""],
              "topics": [" Game Pass ", null, "game pass", "PlayStation 5"]
            }
            """);

        // Act
        var result = GeminiMetadataExtractionService.ParseExtractedMetadata(response);

        // Assert
        Equal(expected, result.Hosts.ToArray());
        Equal(expectedArray, result.Guests.ToArray());
        Equal(expectedArray0, result.Topics.ToArray());
    }

    private static GenerateContentResponse CreateResponse(string jsonText)
    {
        return new GenerateContentResponse
        {
            Candidates =
            [
                new Candidate
                {
                    Content = new Content
                    {
                        Parts =
                        [
                            new Part { Text = jsonText }
                        ]
                    }
                }
            ]
        };
    }
}

