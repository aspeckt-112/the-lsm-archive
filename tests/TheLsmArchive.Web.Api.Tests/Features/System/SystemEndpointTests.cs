using System.Net;
using System.Net.Http.Json;

using Moq;

namespace TheLsmArchive.Web.Api.Tests.Features.System;

public class SystemEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SystemEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetLastDataSyncDateTime_ReturnsOk()
    {
        // Arrange
        DateTimeOffset expected = new(2024, 6, 15, 12, 30, 0, TimeSpan.Zero);
        _factory.SystemServiceMock
            .Setup(s => s.GetLastDataSyncDateTimeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/system/last-data-sync");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DateTimeOffset result = await response.Content.ReadFromJsonAsync<DateTimeOffset>();
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetLastDataSyncDateTime_WhenServiceThrows_Returns500()
    {
        // Arrange
        _factory.SystemServiceMock
            .Setup(s => s.GetLastDataSyncDateTimeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No linked posts found."));

        // Act
        HttpResponseMessage response = await _client.GetAsync("/system/last-data-sync");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
