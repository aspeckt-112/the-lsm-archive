using TheLsmArchive.Models.Request.Abstractions;
using TheLsmArchive.Web.Api.Infrastructure;

namespace TheLsmArchive.Web.Api.Tests.Infrastructure;

public class ExtensionsTests
{
    private record TestPagedRequest(int PageNumber, int PageSize) : PagedRequest(PageNumber, PageSize);

    [Fact]
    public void IQueryable_WithPaging_ReturnsCorrectSubset()
    {
        // Arrange
        IQueryable<int> data = Enumerable.Range(1, 100).AsQueryable();
        var pagedRequest = new TestPagedRequest(2, 10); // Page 2 with page size of 10

        // Act
        var result = data.WithPaging(pagedRequest).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal(11, result.First());
        Assert.Equal(20, result.Last());
    }

    [Fact]
    public void IQueryable_WithPaging_ReturnsEmptyForOutOfRangePage()
    {
        // Arrange
        IQueryable<int> data = Enumerable.Range(1, 100).AsQueryable();
        var pagedRequest = new TestPagedRequest(11, 10); // Page 11 with page size of 10 (out of range)

        // Act
        var result = data.WithPaging(pagedRequest).ToList();

        // Assert
        Assert.Empty(result);
    }
}
