
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Web.Api.Infrastructure;

namespace TheLsmArchive.Web.Api.Tests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler;
    public GlobalExceptionHandlerTests()
    {
        Mock<ILogger<GlobalExceptionHandler>> mockLogger = new();

        Mock<IProblemDetailsService> mockProblemDetailsService = new();
        mockProblemDetailsService
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        _handler = new GlobalExceptionHandler(
            mockLogger.Object,
            mockProblemDetailsService.Object);
    }

    [Fact]
    public async Task TryHandleAsync_WithArgumentOutOfRangeException_ReturnsBadRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new ArgumentOutOfRangeException("param", "Value is out of range.");

        // Act
        bool result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Something went wrong.");

        // Act
        bool result = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
    }
}
