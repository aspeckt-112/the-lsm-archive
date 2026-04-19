using System.Net.Http.Headers;
using System.Threading.RateLimiting;

using Google.GenAI.Types;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Polly;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

using Serilog;

using TheLsmArchive.Database;
using TheLsmArchive.Domain.Services;
using TheLsmArchive.Patreon.Ingestion.Constants;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Services;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;
using TheLsmArchive.Patreon.Ingestion.Services.AI;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Services.RSS;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

EnsureConfiguredLogDirectoryExists(builder);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddOptionsWithValidateOnStart<GeminiOptions>()
    .BindConfiguration(nameof(GeminiOptions))
    .ValidateDataAnnotations();

builder.Services.AddOptionsWithValidateOnStart<RssFeedSources>()
    .BindConfiguration(nameof(RssFeedSources))
    .ValidateDataAnnotations();

builder.Services.AddOptionsWithValidateOnStart<UserAgentOptions>()
    .BindConfiguration(nameof(UserAgentOptions))
    .ValidateDataAnnotations();

builder.Services.AddOptionsWithValidateOnStart<PatreonIngestionOptions>()
    .BindConfiguration(nameof(PatreonIngestionOptions))
    .ValidateDataAnnotations();

builder.Services
    .AddSingleton<IAiSummaryService, GeminiSummaryService>()
    .AddHostedService<PatreonIngestionService>()
    .AddSingleton<PatreonRssParser>()
    .AddSingleton<PromptService>()
    .AddSingleton<ShowService>()
    .AddSingleton<PatreonService>();

builder.Services.AddResiliencePipeline(
   Constants.AiSummaryPipelineName,
    resiliencePipelineBuilder =>
    {
        resiliencePipelineBuilder
            .AddRateLimiter(new SlidingWindowRateLimiter(
                new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 60, // 60 segments of 1 second each
                    PermitLimit = 1000, // Max 1000 requests per minute (Gemini Flash limit)
                    QueueLimit = 100
                }))
            .AddTimeout(TimeSpan.FromSeconds(3600)) // 1 hour timeout
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<RateLimiterRejectedException>() // Retry on rate limit rejections
            });
    });

builder.Services
    .AddHttpClient<PatreonRssParser>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.Clear();

        UserAgentOptions userAgentOptions =
            builder.Configuration.GetSection(nameof(UserAgentOptions)).Get<UserAgentOptions>()
            ?? throw new InvalidOperationException("UserAgentOptions section is missing in configuration.");

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                userAgentOptions.UserAgent,
                userAgentOptions.Version));

        client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml;q=0.9, */*;q=0.8");
    });

builder.Services.AddSingleton(sp =>
{
    IOptions<GeminiOptions> options = sp.GetRequiredService<IOptions<GeminiOptions>>();
    GeminiOptions optionsValue = options.Value;

    const int oneHourInMilliseconds = 3_600_000;

    HttpOptions httpOptions = new()
    {
        Timeout = oneHourInMilliseconds
    };

    return new Google.GenAI.Client(apiKey: optionsValue.ApiKey, httpOptions: httpOptions);
});

builder.Services.AddDbContextFactory(builder.Configuration);

IHost host = builder.Build();

await host.RunAsync();

static void EnsureConfiguredLogDirectoryExists(HostApplicationBuilder builder)
{
    string? configuredPath = builder.Configuration["Serilog:WriteTo:FileSink:Args:path"];

    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return;
    }

    string fullPath = Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.Combine(builder.Environment.ContentRootPath, configuredPath);

    string? logDirectory = Path.GetDirectoryName(fullPath);

    if (!string.IsNullOrWhiteSpace(logDirectory))
    {
        Directory.CreateDirectory(logDirectory);
    }
}
