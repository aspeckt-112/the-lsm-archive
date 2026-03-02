using Microsoft.AspNetCore.HttpOverrides;

using TheLsmArchive.Database;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Search;
using TheLsmArchive.Web.Api.Features.Topics;
using TheLsmArchive.Web.Api.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContext(builder.Configuration, ServiceLifetime.Scoped)
    .AddProblemDetails()
    .AddScoped<ISearchService, SearchService>()
    .AddScoped<IEpisodeService, EpisodeService>()
    .AddScoped<IPersonService, PersonService>()
    .AddScoped<ITopicService, TopicService>()
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

app.UseExceptionHandler();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    await using ReadWriteDbContext dbContext = scope.ServiceProvider.GetRequiredService<ReadWriteDbContext>();
    await dbContext.Database.MigrateAsync();
}

app
    .UseCors()
    .UseHttpsRedirection();

app
    .AddSearchEndpoints()
    .AddEpisodeEndpoints()
    .AddTopicEndpoints()
    .AddPersonEndpoints();

await app.RunAsync();
