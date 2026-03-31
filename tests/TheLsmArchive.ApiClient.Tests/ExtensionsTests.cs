using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;

namespace TheLsmArchive.ApiClient.Tests;

public class ExtensionsTests
{
    [Fact]
    public void SearchRequest_ToQueryString_IncludesAllFields()
    {
        SearchRequest request = new("dark souls", EntityType.Episode, PageNumber: 2, PageSize: 25);

        string result = request.ToQueryString();

        Assert.Equal("?searchTerm=dark%20souls&entityType=Episode&pageNumber=2&pageSize=25", result);
    }

    [Fact]
    public void SearchRequest_ToQueryString_OmitsSearchTermWhenEmpty()
    {
        SearchRequest request = new("", EntityType.All, PageNumber: 1, PageSize: 50);

        string result = request.ToQueryString();

        Assert.Equal("?entityType=All&pageNumber=1&pageSize=50", result);
    }

    [Fact]
    public void SearchRequest_ToQueryString_OmitsSearchTermWhenWhitespace()
    {
        SearchRequest request = new("   ", EntityType.Person, PageNumber: 1, PageSize: 10);

        string result = request.ToQueryString();

        Assert.Equal("?entityType=Person&pageNumber=1&pageSize=10", result);
    }

    [Fact]
    public void SearchRequest_ToQueryString_EncodesSpecialCharacters()
    {
        SearchRequest request = new("C# & .NET", EntityType.Topic);

        string result = request.ToQueryString();

        Assert.Contains("searchTerm=C%23%20%26%20.NET", result);
    }

    [Fact]
    public void SearchRequest_ToQueryString_EncodesUnicodeCharacters()
    {
        SearchRequest request = new("café", EntityType.All);

        string result = request.ToQueryString();

        Assert.Contains("searchTerm=caf%C3%A9", result);
    }

    [Fact]
    public void PagedItemRequest_ToQueryString_IncludesPageNumberAndPageSize()
    {
        PagedItemRequest request = new(PageNumber: 3, PageSize: 20);

        string result = request.ToQueryString();

        Assert.Equal("?pageNumber=3&pageSize=20", result);
    }

    [Fact]
    public void PagedItemRequest_ToQueryString_IncludesSearchTermWhenProvided()
    {
        PagedItemRequest request = new(PageNumber: 1, PageSize: 10, SearchTerm: "test query");

        string result = request.ToQueryString();

        Assert.Equal("?pageNumber=1&pageSize=10&searchTerm=test%20query", result);
    }

    [Fact]
    public void PagedItemRequest_ToQueryString_OmitsSearchTermWhenNull()
    {
        PagedItemRequest request = new(PageNumber: 1, PageSize: 50, SearchTerm: null);

        string result = request.ToQueryString();

        Assert.Equal("?pageNumber=1&pageSize=50", result);
    }

    [Fact]
    public void PagedItemRequest_ToQueryString_OmitsSearchTermWhenEmpty()
    {
        PagedItemRequest request = new(PageNumber: 1, PageSize: 50, SearchTerm: "");

        string result = request.ToQueryString();

        Assert.Equal("?pageNumber=1&pageSize=50", result);
    }

    [Fact]
    public void PagedItemRequest_ToQueryString_EncodesSpecialCharacters()
    {
        PagedItemRequest request = new(PageNumber: 1, PageSize: 10, SearchTerm: "a&b=c");

        string result = request.ToQueryString();

        Assert.Contains("searchTerm=a%26b%3Dc", result);
    }
}
