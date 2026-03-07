using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;

namespace TheLsmArchive.Web.Api.Tests.Features.Episodes;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class EpisodeServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly EpisodeService _episodeService;

    public EpisodeServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<EpisodeService>> loggerMock = new();

        _episodeService = new EpisodeService(
            loggerMock.Object,
            ReadOnlyDbContext
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetById_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange & Act & Assert
#pragma warning disable IDE0022 // Use expression body for method
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _episodeService.GetById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022 // Use expression body for method
    }

    [Fact]
    public async Task GetById_WithValidIdButNonExistentEpisode_ReturnsNull()
    {
        // Arrange & Act
        Episode? episode = await _episodeService.GetById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(episode);
    }
}
