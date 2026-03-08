using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;

using Google.GenAI;
using Google.GenAI.Types;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

using Content = Google.GenAI.Types.Content;
using GenAI = Google.GenAI.Types;
using Part = Google.GenAI.Types.Part;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The Gemini AI summary service implementation.
/// </summary>
public sealed partial class GeminiSummaryService : IAiSummaryService
{
    private const string HostsPropertyName = "hosts";
    private const string GuestsPropertyName = "guests";
    private const string TopicsPropertyName = "topics";

    private readonly ILogger<GeminiSummaryService> _logger;

    private readonly Client _client;

    private readonly PromptService _promptService;

    private readonly string _model;


    private static readonly Schema _responseSchema = new()
    {
        Type = GenAI.Type.OBJECT,
        Properties = new Dictionary<string, Schema>
        {
            { HostsPropertyName, new Schema { Type = GenAI.Type.ARRAY, Items = new Schema { Type = GenAI.Type.STRING } } },
            { GuestsPropertyName, new Schema { Type = GenAI.Type.ARRAY, Items = new Schema { Type = GenAI.Type.STRING } } },
            { TopicsPropertyName, new Schema { Type = GenAI.Type.ARRAY, Items = new Schema { Type = GenAI.Type.STRING } } }
        },
        Required = [HostsPropertyName, GuestsPropertyName, TopicsPropertyName]
    };

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiSummaryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="client">The Gemini client.</param>
    /// <param name="options">The Gemini options.</param>
    /// <param name="promptService">The prompt service.</param>
    public GeminiSummaryService(
        ILogger<GeminiSummaryService> logger,
        Client client,
        IOptions<GeminiOptions> options,
        PromptService promptService)
    {
        _logger = logger;
        _client = client;
        _promptService = promptService;
        _model = options.Value.Model;
    }

    /// <inheritdoc/>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public async Task<AiSummary> GenerateAiSummaryFromPatreonPost(
        ShowEntity show,
        PatreonPostEntity patreonPost,
        CancellationToken cancellationToken,
        IEnumerable<string>? knownHosts = null,
        IEnumerable<string>? knownTopics = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string systemPromptText = _promptService.GetSummarySystemPrompt(
            show.Name,
            knownHosts,
            knownTopics);

        var systemInstruction = new Content { Parts = [new Part { Text = systemPromptText }] };

        var userContent = new Content
        {
            Role = "user",
            Parts =
            [
                new Part { Text = $"Title: {patreonPost.Title}\nDescription: {StripHtml(patreonPost.Summary)}" }
            ]
        };

        try
        {
            GenerateContentResponse response = await _client.Models.GenerateContentAsync(
                _model,
                userContent,
                new GenerateContentConfig
                {
                    ResponseMimeType = MediaTypeNames.Application.Json,
                    ResponseSchema = _responseSchema,
                    SystemInstruction = systemInstruction,
                    Temperature = 0.4f
                });

            // Safe response parsing with guards
            if (response.Candidates?.Count == 0)
            {
                _logger.LogWarning(
                    "Gemini returned no candidates for post {PostId}",
                    patreonPost.Id);

                throw new InvalidOperationException("Received empty candidates from Gemini.");
            }

            Content? content = response!.Candidates![0].Content;

            if (content?.Parts?.Count == 0)
            {
                _logger.LogWarning(
                    "Gemini returned no content parts for post {PostId}",
                    patreonPost.Id);

                throw new InvalidOperationException("Received empty content parts from Gemini.");
            }

            string? jsonText = content!.Parts![0].Text;

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Gemini returned empty JSON for post {PostId}", patreonPost.Id);
                throw new InvalidOperationException("Received empty response from Gemini.");
            }

            // Deserialize
            GeminiResponseDto? resultDto = JsonSerializer.Deserialize<GeminiResponseDto>(
                jsonText,
                _jsonSerializerOptions);

            if (resultDto == null)
            {
                _logger.LogWarning("Gemini returned null JSON for post {PostId}", patreonPost.Id);
                throw new InvalidOperationException("Failed to deserialize Gemini response.");
            }

            return new AiSummary(
                resultDto.Hosts,
                resultDto.Guests,
                resultDto.Topics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary for post {Title}", patreonPost.Title);
            throw;
        }
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remove HTML tags using regex
        string stripped = HtmlTagRegex().Replace(input, string.Empty);

        // Decode HTML entities
        stripped = WebUtility.HtmlDecode(stripped);

        // Normalize whitespace
        stripped = WhitespaceRegex().Replace(stripped, " ").Trim();

        return stripped;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagRegex();
}
