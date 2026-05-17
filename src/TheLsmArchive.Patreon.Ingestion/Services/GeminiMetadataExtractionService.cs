using System.Net.Mime;
using System.Text.Json;

using Google.GenAI;
using Google.GenAI.Types;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Registry;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

using Content = Google.GenAI.Types.Content;
using GenAI = Google.GenAI.Types;
using Part = Google.GenAI.Types.Part;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The Gemini metadata extraction service implementation.
/// </summary>
public sealed class GeminiMetadataExtractionService : IMetadataExtractionService
{
    private const string HostsPropertyName = "hosts";
    private const string GuestsPropertyName = "guests";
    private const string TopicsPropertyName = "topics";

    private readonly ILogger<GeminiMetadataExtractionService> _logger;
    private readonly Client _client;
    private readonly MetadataExtractionPromptBuilder _promptBuilder;
    private readonly string _model;
    private readonly ResiliencePipeline _metadataExtractionPipeline;

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Schema _responseSchema = new()
    {
        Type = GenAI.Type.Object,
        Properties = new Dictionary<string, Schema>
        {
            {
                HostsPropertyName,
                new Schema { Type = GenAI.Type.Array, Items = new Schema { Type = GenAI.Type.String } }
            },
            {
                GuestsPropertyName,
                new Schema { Type = GenAI.Type.Array, Items = new Schema { Type = GenAI.Type.String } }
            },
            {
                TopicsPropertyName,
                new Schema { Type = GenAI.Type.Array, Items = new Schema { Type = GenAI.Type.String } }
            }
        },
        Required = [HostsPropertyName, GuestsPropertyName, TopicsPropertyName]
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiMetadataExtractionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="client">The Gemini client.</param>
    /// <param name="options">The Gemini options.</param>
    /// <param name="promptBuilder">The metadata extraction prompt builder.</param>
    /// <param name="pipelineProvider">The resilience pipeline provider.</param>
    public GeminiMetadataExtractionService(
        ILogger<GeminiMetadataExtractionService> logger,
        Client client,
        IOptions<GeminiOptions> options,
        MetadataExtractionPromptBuilder promptBuilder,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _logger = logger;
        _client = client;
        _promptBuilder = promptBuilder;
        _model = options.Value.Model;
        _metadataExtractionPipeline = pipelineProvider.GetPipeline(nameof(GeminiMetadataExtractionService));
    }

    /// <inheritdoc/>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public async Task<AiSummary> ExtractMetadataAsync(
        ShowEntity show,
        PatreonPostEntity patreonPost,
        CancellationToken cancellationToken,
        IList<string>? knownPersons = null,
        IList<string>? knownTopics = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string systemPromptText = _promptBuilder.BuildSystemPrompt(
            show.Name,
            knownPersons,
            knownTopics);

        var systemInstruction = new Content { Parts = [new Part { Text = systemPromptText }] };

        var userContent = new Content
        {
            Role = "user",
            Parts =
            [
                new Part
                {
                    Text =
                        $"Title: {patreonPost.Title}\nDescription: {Helpers.HtmlSanitizer.StripHtml(patreonPost.Summary)}"
                }
            ]
        };

        ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

        try
        {
            return await _metadataExtractionPipeline.ExecuteAsync(
                async context =>
                {
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
                            context.CancellationToken);

                        return ParseExtractedMetadata(response);
                    }
                    catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (InvalidDataException ex)
                    {
                        _logger.LogWarning(
                            "Gemini returned invalid metadata for post {PostId}: {ErrorMessage}",
                            patreonPost.Id,
                            ex.Message);

                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract metadata for post {Title}", patreonPost.Title);
                        throw;
                    }
                },
                resilienceContext);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(resilienceContext);
        }
    }

    internal static AiSummary ParseExtractedMetadata(GenerateContentResponse response)
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
        if (values is null)
        {
            return [];
        }

        return
        [
            .. values.Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }
}

