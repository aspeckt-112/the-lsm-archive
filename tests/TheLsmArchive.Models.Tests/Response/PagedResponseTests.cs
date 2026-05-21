using TheLsmArchive.Models.Response;

namespace TheLsmArchive.Models.Tests.Response;

public class PagedResponseTests
{
    [Theory]
    [InlineData(100, 25, 4)]
    [InlineData(101, 25, 5)]
    [InlineData(0, 25, 0)]
    public void TotalPages_ReturnsExpectedValue(int totalCount, int pageSize, int expected)
    {
        // Arrange
        PagedResponse<int> response = new([], totalCount, 1, pageSize);

        // Act
        int totalPages = response.TotalPages;

        // Assert
        Equal(expected, totalPages);
    }
}
