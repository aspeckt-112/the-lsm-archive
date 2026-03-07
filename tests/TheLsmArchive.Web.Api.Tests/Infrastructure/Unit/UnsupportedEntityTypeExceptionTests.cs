using Moq;

using TheLsmArchive.Models.Enums;
using TheLsmArchive.Web.Api.Features.Search;

namespace TheLsmArchive.Web.Api.Tests.Infrastructure.Unit;

public class UnsupportedEntityTypeExceptionTests
{
    [Fact]
    public void For_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        EntityType entityType = It.IsAny<EntityType>();

        // Act
        var exception = UnsupportedEntityTypeException.For(entityType);

        // Assert
        Assert.Equal($"Unsupported entity type: {entityType}", exception.Message);
    }
}
