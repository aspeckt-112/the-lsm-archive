using TheLsmArchive.Models.Enums;
using TheLsmArchive.Web.Api.Features.Search;

namespace TheLsmArchive.Web.Api.Tests.Features.Search;

public sealed class UnsupportedEntityTypeExceptionTests
{
    [Fact]
    public void For_WhenEntityTypeIsUnsupported_ReturnsExceptionWithExpectedMessage()
    {
        // Act
        var exception = UnsupportedEntityTypeException.For((EntityType)999);

        // Assert
        IsType<UnsupportedEntityTypeException>(exception);
        Equal("Unsupported entity type: 999", exception.Message);
    }
}
