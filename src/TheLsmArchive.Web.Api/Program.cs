using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Serilog;

using TheLsmArchive.Database;
using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Search;
using TheLsmArchive.Web.Api.Features.System;
using TheLsmArchive.Web.Api.Features.Topics;
using TheLsmArchive.Web.Api.Infrastructure;
using TheLsmArchive.Web.Api.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.WebHost.UseSentry();

builder.Services
    .AddOptionsWithValidateOnStart<CorsSettingsOptions>()
    .BindConfiguration(CorsSettingsOptions.SectionName)
    .ValidateDataAnnotations();

builder.Services
    .AddOptions<CorsOptions>()
    .Configure<IOptions<CorsSettingsOptions>, IWebHostEnvironment>((
        corsOptions,
        corsSettingsOptionsAccessor,
        environment) =>
    {
        corsOptions.AddDefaultPolicy(policy =>
        {
            if (environment.IsDevelopment())
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                return;
            }

            CorsSettingsOptions corsSettingsOptions = corsSettingsOptionsAccessor.Value;

            policy.WithOrigins(corsSettingsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

builder.Services
    .AddDbContext(builder.Configuration, ServiceLifetime.Scoped)
    .AddProblemDetails()
    .AddScoped<ISearchService, SearchService>()
    .AddScoped<IEpisodeService, EpisodeService>()
    .AddScoped<IPersonService, PersonService>()
    .AddScoped<ITopicService, TopicService>()
    .AddScoped<ISystemService, SystemService>()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddCors();

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

/// <summary>
/// Exposes the web application's entry point to integration tests.
/// </summary>
public partial class Program;
