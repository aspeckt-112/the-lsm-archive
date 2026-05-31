using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;

namespace TheLsmArchive.ApiClient.Tests;

public class ExtensionsTests
{
    [Fact]
    public void ToQueryString_ForSearchRequest_WithSearchTerm_IncludesEncodedSearchTermAndPaging()
    {
        // Arrange
        SearchRequest request = new("Sacred Symbols & Friends", EntityType.Person, 2, 25);

        // Act
        string queryString = request.ToQueryString();

        // Assert
        Equal(
            "?searchTerm=Sacred%20Symbols%20%26%20Friends&entityType=Person&pageNumber=2&pageSize=25",
            queryString);
    }

    [Fact]
    public void ToQueryString_ForSearchRequest_WithWhitespaceSearchTerm_OmitsSearchTerm()
    {
        // Arrange
        SearchRequest request = new("   ", EntityType.Topic, 3, 10);

        // Act
        string queryString = request.ToQueryString();

        // Assert
        Equal("?entityType=Topic&pageNumber=3&pageSize=10", queryString);
    }

    [Fact]
    public void ToQueryString_ForPagedItemRequest_WithSearchTerm_IncludesEncodedSearchTerm()
    {
        // Arrange
        PagedItemRequest request = new(4, 15, "Colin Moriarty / Defining Duke");

        // Act
        string queryString = request.ToQueryString();

        // Assert
        Equal(
            "?pageNumber=4&pageSize=15&searchTerm=Colin%20Moriarty%20%2F%20Defining%20Duke",
            queryString);
    }

    [Fact]
    public void ToQueryString_ForPagedItemRequest_WithWhitespaceSearchTerm_OmitsSearchTerm()
    {
        // Arrange
        PagedItemRequest request = new(1, 50, "   ");

        // Act
        string queryString = request.ToQueryString();

        // Assert
        Equal("?pageNumber=1&pageSize=50", queryString);
    }
}
