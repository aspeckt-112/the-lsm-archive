using Microsoft.AspNetCore.HttpOverrides;

using Serilog;

using TheLsmArchive.Database;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Search;
using TheLsmArchive.Web.Api.Features.System;
using TheLsmArchive.Web.Api.Features.Topics;
using TheLsmArchive.Web.Api.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

EnsureConfiguredLogDirectoryExists(builder);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services
    .AddDbContext(builder.Configuration, ServiceLifetime.Scoped)
    .AddProblemDetails()
    .AddScoped<ISearchService, SearchService>()
    .AddScoped<IEpisodeService, EpisodeService>()
    .AddScoped<IPersonService, PersonService>()
    .AddScoped<ITopicService, TopicService>()
    .AddScoped<ISystemService, SystemService>()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddCors(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        }
        else
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://lsmarchive.com", "https://www.lsmarchive.com")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        }
    });

WebApplication app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await using LsmArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<LsmArchiveDbContext>();
    await dbContext.Database.MigrateAsync();
}

app
    .UseCors()
    .UseHttpsRedirection();

app
    .AddSearchEndpoints()
    .AddEpisodeEndpoints()
    .AddTopicEndpoints()
    .AddPersonEndpoints()
    .AddSystemEndpoints();

await app.RunAsync();

static void EnsureConfiguredLogDirectoryExists(WebApplicationBuilder builder)
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
