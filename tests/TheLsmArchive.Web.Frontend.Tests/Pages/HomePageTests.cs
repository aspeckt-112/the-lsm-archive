using Bunit;

using Microsoft.JSInterop;

using TheLsmArchive.ApiClient;
using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Frontend.Pages;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Pages;

public sealed class HomePageTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_OnInitialized_LoadsRecentEpisodesAndLastSyncDate()
    {
        // Arrange
        RenderProviders();
        DateTimeOffset lastSync = new(2026, 5, 29, 12, 30, 0, TimeSpan.Zero);
        List<Episode> recentEpisodes =
        [
            FrontendTestData.CreateEpisode(1, "Recent Episode 1"),
            FrontendTestData.CreateEpisode(2, "Recent Episode 2")
        ];

        ClientService.GetLastDataSyncDateTimeAsyncHandler = _ => Task.FromResult(Result<DateTimeOffset>.Ok(lastSync));
        ClientService.GetRecentEpisodesHandler = _ => Task.FromResult(Result<List<Episode>>.Ok(recentEpisodes));

        // Act
        var component = RenderComponent<HomePage>();

        // Assert
        component.WaitForAssertion(() =>
        {
            Contains("Recent Episodes", component.Markup, StringComparison.Ordinal);
            Contains("Recent Episode 1", component.Markup, StringComparison.Ordinal);
            Contains("Recent Episode 2", component.Markup, StringComparison.Ordinal);
            Contains("Last updated:", component.Markup, StringComparison.Ordinal);
        });

        Equal("Home", BreadcrumbService.Breadcrumbs.Single().Text);
    }

    [Fact]
    public void Search_WhenSearchReturnsResults_GroupsAndRendersEntityCards()
    {
        // Arrange
        RenderProviders();
        SetupHomeDefaults();

        List<SearchResult> firstPageItems =
        [
            FrontendTestData.CreateSearchResult(10, "Colin Moriarty", EntityType.Person),
            FrontendTestData.CreateSearchResult(11, "Sacred Symbols", EntityType.Episode)
        ];
        List<SearchResult> secondPageItems =
        [
            FrontendTestData.CreateSearchResult(12, "PlayStation", EntityType.Topic)
        ];

        int searchCallCount = 0;
        ClientService.SearchHandler = (request, _) =>
        {
            searchCallCount++;

            if (request.PageNumber == 1)
            {
                return Task.FromResult(Result<PagedResponse<SearchResult>>.Ok(new PagedResponse<SearchResult>(firstPageItems, 51, 1, 50)));
            }

            return Task.FromResult(Result<PagedResponse<SearchResult>>.Ok(new PagedResponse<SearchResult>(secondPageItems, 51, 2, 50)));
        };

        var component = RenderComponent<HomePage>();

        // Act
        component.Find("input").Input("colin");

        // Assert
        component.WaitForAssertion(() =>
        {
            Contains("Person", component.Markup, StringComparison.Ordinal);
            Contains("Episode", component.Markup, StringComparison.Ordinal);
            Contains("Topic", component.Markup, StringComparison.Ordinal);
            Contains("Colin Moriarty", component.Markup, StringComparison.Ordinal);
            Contains("Sacred Symbols", component.Markup, StringComparison.Ordinal);
            Contains("PlayStation", component.Markup, StringComparison.Ordinal);
        }, timeout: TimeSpan.FromSeconds(2));

        Equal(2, searchCallCount);
    }

    [Fact]
    public void Search_WhenNoMatchesExist_ShowsEmptyState()
    {
        // Arrange
        RenderProviders();
        SetupHomeDefaults();
        ClientService.SearchHandler = (_, _) => Task.FromResult(Result<PagedResponse<SearchResult>>.None());

        var component = RenderComponent<HomePage>();

        // Act
        component.Find("input").Input("does-not-exist");

        // Assert
        component.WaitForAssertion(() =>
        {
            Contains("No results found", component.Markup, StringComparison.Ordinal);
            Contains("does-not-exist", component.Markup, StringComparison.Ordinal);
        }, timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Search_WhenFailureOccurs_ShowsSnackbarMessage()
    {
        // Arrange
        RenderProviders();
        SetupHomeDefaults();
        ClientService.SearchHandler = (_, _) => Task.FromResult(Result<PagedResponse<SearchResult>>.Fail("Search is unavailable."));

        var component = RenderComponent<HomePage>();

        // Act
        component.Find("input").Input("failure");

        // Assert
        component.WaitForAssertion(() => Equal("Search is unavailable.", Snackbar.ShownSnackbars.Single().Message), timeout: TimeSpan.FromSeconds(2));
        Contains("Search the archive", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void RandomEpisode_WhenEpisodeExists_NavigatesToEpisodePage()
    {
        // Arrange
        RenderProviders();
        SetupHomeDefaults();
        ClientService.GetRandomEpisodeIdHandler = _ => Task.FromResult(Result<int>.Ok(123));

        var component = RenderComponent<HomePage>();

        // Act
        component.Find("button.random-episode-button").Click();

        // Assert
        component.WaitForAssertion(() => Equal("https://localhost/Episode/123", NavigationManager.Uri));
    }

    [Fact]
    public void RandomEpisode_WhenNoEpisodeExists_DoesNotNavigate()
    {
        // Arrange
        RenderProviders();
        SetupHomeDefaults();
        ClientService.GetRandomEpisodeIdHandler = _ => Task.FromResult(Result<int>.None());

        var component = RenderComponent<HomePage>();
        string startingUri = NavigationManager.Uri;

        // Act
        component.Find("button.random-episode-button").Click();

        // Assert
        component.WaitForAssertion(() => Equal(startingUri, NavigationManager.Uri));
    }

    private void SetupHomeDefaults()
    {
        ClientService.GetLastDataSyncDateTimeAsyncHandler = _ => Task.FromResult(Result<DateTimeOffset>.None());
        ClientService.GetRecentEpisodesHandler = _ => Task.FromResult(Result<List<Episode>>.Ok([]));
    }
}




