using System.Net.Mime;
using System.Text.Json;

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

namespace TheLsmArchive.Patreon.Ingestion.Services.AI;

/// <summary>
/// The Gemini AI summary service implementation.
/// </summary>
public sealed class GeminiSummaryService : IAiSummaryService
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
        Type = GenAI.Type.Object,
        Properties = new Dictionary<string, Schema>
        {
            { HostsPropertyName, new Schema { Type = GenAI.Type.Array, Items = new Schema { Type = GenAI.Type.String } } },
            { GuestsPropertyName, new Schema { Type = GenAI.Type.Array, Items = new Schema { Type = GenAI.Type.String } } },
            { TopicsPropertyName, new Schema { Type = GenAI.Type.Array, Items = new Schema { Type = GenAI.Type.String } } }
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
        IList<string>? knownPersons = null,
        IList<string>? knownTopics = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string systemPromptText = _promptService.GetSummarySystemPrompt(
            show.Name,
            knownPersons,
            knownTopics);

        var systemInstruction = new Content { Parts = [new Part { Text = systemPromptText }] };

        var userContent = new Content
        {
            Role = "user",
            Parts =
            [
                new Part { Text = $"Title: {patreonPost.Title}\nDescription: {Helpers.HtmlSanitizer.StripHtml(patreonPost.Summary)}" }
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
                    Temperature = 0.5f
                },
                cancellationToken);

            return ParseAiSummary(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(
                "Gemini returned an invalid response for post {PostId}: {ErrorMessage}",
                patreonPost.Id,
                ex.Message);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary for post {Title}", patreonPost.Title);
            throw;
        }
    }

    internal static AiSummary ParseAiSummary(GenerateContentResponse response)
    {
        string jsonText = ExtractJsonText(response);

        GeminiResponseDto resultDto;

        try
        {
            resultDto = JsonSerializer.Deserialize<GeminiResponseDto>(
                jsonText,
                _jsonSerializerOptions)
                ?? throw new InvalidDataException("Failed to deserialize Gemini response.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Failed to deserialize Gemini response.", ex);
        }

        return CreateAiSummary(resultDto);
    }

    private static string ExtractJsonText(GenerateContentResponse response)
    {
        if (response.Candidates is not { Count: > 0 })
        {
            throw new InvalidDataException("Received empty candidates from Gemini.");
        }

        Content? content = response.Candidates[0].Content;

        if (content?.Parts is not { Count: > 0 })
        {
            throw new InvalidDataException("Received empty content parts from Gemini.");
        }

        Part firstPart = content.Parts[0];
        string? jsonText = firstPart.Text;

        return !string.IsNullOrWhiteSpace(jsonText)
            ? jsonText
            : throw new InvalidDataException("Received empty response from Gemini.");
    }

    private static AiSummary CreateAiSummary(GeminiResponseDto resultDto)
    {
        string[] hosts = SanitizeValues(resultDto.Hosts);
        string[] guests =
        [
            .. SanitizeValues(resultDto.Guests)
                .Except(hosts, StringComparer.OrdinalIgnoreCase)
        ];
        string[] topics = SanitizeValues(resultDto.Topics);

        return new AiSummary(hosts, guests, topics);
    }

    private static string[] SanitizeValues(IEnumerable<string?>? values)
    {
        return values is null
            ? []
            :
            [
                .. values
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
    }
}
