using System.Net;
using System.Net.Http.Json;

using Moq;

using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.Web.Api.Tests.Features.Search;

public class SearchEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SearchEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithResults_ReturnsOk()
    {
        // Arrange
        PagedResponse<SearchResult> expected = new(
            [new(1, "Colin Moriarty", EntityType.Person)], 1, 1, 50);
        _factory.SearchServiceMock
            .Setup(s => s.RunSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/search?SearchTerm=Colin&EntityType=All");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PagedResponse<SearchResult>? result = await response.Content.ReadFromJsonAsync<PagedResponse<SearchResult>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Search_WithNoResults_ReturnsNoContent()
    {
        // Arrange
        PagedResponse<SearchResult> expected = new([], 0, 1, 50);
        _factory.SearchServiceMock
            .Setup(s => s.RunSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/search?SearchTerm=nonexistent&EntityType=All");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithPersonFilter_ReturnsOk()
    {
        // Arrange
        PagedResponse<SearchResult> expected = new(
            [new(1, "Colin Moriarty", EntityType.Person)], 1, 1, 50);
        _factory.SearchServiceMock
            .Setup(s => s.RunSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/search?SearchTerm=Colin&EntityType=Person");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_PassesQueryParametersToService()
    {
        // Arrange
        PagedResponse<SearchResult> expected = new([], 0, 1, 50);
        SearchRequest? capturedRequest = null;
        _factory.SearchServiceMock
            .Setup(s => s.RunSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SearchRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(expected);

        // Act
        await _client.GetAsync("/search?SearchTerm=test&EntityType=Topic&PageNumber=2&PageSize=10");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("test", capturedRequest.SearchTerm);
        Assert.Equal(EntityType.Topic, capturedRequest.EntityType);
        Assert.Equal(2, capturedRequest.PageNumber);
        Assert.Equal(10, capturedRequest.PageSize);
    }
}
