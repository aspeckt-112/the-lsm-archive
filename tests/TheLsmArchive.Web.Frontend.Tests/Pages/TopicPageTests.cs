using Bunit;

using TheLsmArchive.ApiClient;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Frontend.Pages;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Pages;

public sealed class TopicPageTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenTopicPageLoads_ShowsTopicTimelineAndMostDiscussedAlongside()
    {
        // Arrange
        RenderProviders();
        Topic topic = FrontendTestData.CreateTopic(7, "PlayStation");
        TopicTimeline timeline = FrontendTestData.CreateTopicTimeline(
            entries: new PagedResponse<TopicTimelineEntry>([FrontendTestData.CreateTopicTimelineEntry(9, "Sacred Symbols 350")], 1, 1, 25));
        List<MostDiscussedTopic> mostDiscussedAlongside =
        [
            FrontendTestData.CreateMostDiscussedTopic(8, "Xbox", 5),
            FrontendTestData.CreateMostDiscussedTopic(9, "Nintendo", 3)
        ];

        ClientService.GetTopicByIdHandler = (_, _) => Task.FromResult(Result<Topic>.Ok(topic));
        ClientService.GetTopicTimelineByIdHandler = (_, _, _, _) => Task.FromResult(Result<TopicTimeline>.Ok(timeline));
        ClientService.GetMostDiscussedAlongsideTopicsByTopicIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok(mostDiscussedAlongside));

        // Act
        var component = RenderComponent<TopicPage>(parameters => parameters.Add(x => x.Id, 7));

        // Assert
        component.WaitForAssertion(() =>
        {
            Contains("PlayStation", component.Markup, StringComparison.Ordinal);
            Contains("Sacred Symbols 350", component.Markup, StringComparison.Ordinal);
            Contains("Xbox", component.Markup, StringComparison.Ordinal);
            Contains("Nintendo", component.Markup, StringComparison.Ordinal);
        });

        Equal("PlayStation", BreadcrumbService.Breadcrumbs[^1].Text);
    }

    [Fact]
    public void ToggleSort_WhenClicked_ReloadsTimelineWithUpdatedSortDirection()
    {
        // Arrange
        RenderProviders();
        List<bool> requestedSortDirections = [];

        ClientService.GetTopicByIdHandler = (_, _) => Task.FromResult(Result<Topic>.Ok(FrontendTestData.CreateTopic(7, "PlayStation")));
        ClientService.GetMostDiscussedAlongsideTopicsByTopicIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok([]));
        ClientService.GetTopicTimelineByIdHandler = (_, pagedRequest, sortDescending, _) =>
        {
            requestedSortDirections.Add(sortDescending);
            return Task.FromResult(Result<TopicTimeline>.Ok(
                FrontendTestData.CreateTopicTimeline(entries: new PagedResponse<TopicTimelineEntry>([FrontendTestData.CreateTopicTimelineEntry(title: $"Page {pagedRequest.PageNumber}")], 1, pagedRequest.PageNumber, pagedRequest.PageSize))));
        };

        var component = RenderComponent<TopicPage>(parameters => parameters.Add(x => x.Id, 7));

        // Act
        component.WaitForAssertion(() => Single(requestedSortDirections));
        component.Find("button.mud-icon-button").Click();

        // Assert
        component.WaitForAssertion(() => Equal(2, requestedSortDirections.Count));
        Equal([true, false], requestedSortDirections);
    }

    [Fact]
    public void Render_WhenTopicRequestFails_ShowsSnackbarMessage()
    {
        // Arrange
        RenderProviders();
        ClientService.GetTopicByIdHandler = (_, _) => Task.FromResult(Result<Topic>.Fail("Topic lookup failed."));
        ClientService.GetTopicTimelineByIdHandler = (_, _, _, _) => Task.FromResult(Result<TopicTimeline>.Ok(FrontendTestData.CreateTopicTimeline()));
        ClientService.GetMostDiscussedAlongsideTopicsByTopicIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok([]));

        // Act
        var component = RenderComponent<TopicPage>(parameters => parameters.Add(x => x.Id, 7));

        // Assert
        component.WaitForAssertion(() => Equal("Topic lookup failed.", Snackbar.ShownSnackbars.Single().Message));
    }
}




