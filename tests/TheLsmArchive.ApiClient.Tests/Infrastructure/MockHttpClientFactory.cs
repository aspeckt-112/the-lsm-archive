using System.Net;
using System.Text;
using System.Text.Json;

using Moq;
using Moq.Protected;

namespace TheLsmArchive.ApiClient.Tests.Infrastructure;

internal static class MockHttpClientFactory
{
    internal static HttpMessageHandler CreateHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
    {
        Mock<HttpMessageHandler> handler = new(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage requestMessage, CancellationToken cancellationToken) =>
                responseFactory(requestMessage, cancellationToken));

        return handler.Object;
    }

    internal static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory,
        string baseAddress = "https://example.com/api/") =>
        new(CreateHandler(responseFactory))
        {
            BaseAddress = new Uri(baseAddress)
        };

    internal static HttpResponseMessage CreateJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new()
        {
            StatusCode = statusCode,
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    internal static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string content,
        string mediaType = "application/json") =>
        new()
        {
            StatusCode = statusCode,
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        };
}

