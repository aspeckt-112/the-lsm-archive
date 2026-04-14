using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Search;
using TheLsmArchive.Web.Api.Features.System;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IEpisodeService> EpisodeServiceMock { get; } = new();
    public Mock<IPersonService> PersonServiceMock { get; } = new();
    public Mock<ITopicService> TopicServiceMock { get; } = new();
    public Mock<ISearchService> SearchServiceMock { get; } = new();
    public Mock<ISystemService> SystemServiceMock { get; } = new();

    public void ResetAllMocks()
    {
        EpisodeServiceMock.Reset();
        PersonServiceMock.Reset();
        TopicServiceMock.Reset();
        SearchServiceMock.Reset();
        SystemServiceMock.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddScoped(_ => EpisodeServiceMock.Object);
            services.AddScoped(_ => PersonServiceMock.Object);
            services.AddScoped(_ => TopicServiceMock.Object);
            services.AddScoped(_ => SearchServiceMock.Object);
            services.AddScoped(_ => SystemServiceMock.Object);
        });
    }
}
