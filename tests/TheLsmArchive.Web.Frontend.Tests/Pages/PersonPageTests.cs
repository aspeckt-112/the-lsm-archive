using Bunit;

using Microsoft.JSInterop;

using TheLsmArchive.ApiClient;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Frontend.Pages;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Pages;

public sealed class PersonPageTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenPersonPageLoads_ShowsPersonDetailsLatestEpisodeTopicsAndTimeline()
    {
        // Arrange
        RenderProviders();
        Person person = FrontendTestData.CreatePerson(42, "Colin Moriarty");
        PersonDetails personDetails = FrontendTestData.CreatePersonDetails(
            firstAppeared: new DateOnly(2020, 1, 1),
            lastAppeared: new DateOnly(2026, 5, 1));
        Episode latestEpisode = FrontendTestData.CreateEpisode(88, "Latest Colin Episode", summaryHtml: "<p>Latest summary.</p>");
        List<MostDiscussedTopic> mostDiscussedTopics =
        [
            FrontendTestData.CreateMostDiscussedTopic(1, "PlayStation", 10),
            FrontendTestData.CreateMostDiscussedTopic(2, "Publishing", 6)
        ];
        PagedResponse<Topic> topicsResponse = new([FrontendTestData.CreateTopic(3, "Games Media")], 1, 1, 50);
        PagedResponse<PersonTimelineEntry> episodesResponse = new([FrontendTestData.CreatePersonTimelineEntry(9, "Sacred Symbols 350")], 1, 1, 25);

        ClientService.GetPersonByIdHandler = (_, _) => Task.FromResult(Result<Person>.Ok(person));
        ClientService.GetPersonDetailsByIdHandler = (_, _) => Task.FromResult(Result<PersonDetails>.Ok(personDetails));
        ClientService.GetLatestEpisodeByPersonIdHandler = (_, _) => Task.FromResult(Result<Episode>.Ok(latestEpisode));
        ClientService.GetMostDiscussedTopicsByPersonIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok(mostDiscussedTopics));
        ClientService.GetTopicsByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<Topic>>.Ok(topicsResponse));
        ClientService.GetEpisodesByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<PersonTimelineEntry>>.Ok(episodesResponse));

        // Act
        var component = RenderComponent<PersonPage>(parameters => parameters.Add(x => x.Id, 42));

        // Assert
        component.WaitForAssertion(() =>
        {
            Contains("Colin Moriarty", component.Markup, StringComparison.Ordinal);
            Contains("Latest Colin Episode", component.Markup, StringComparison.Ordinal);
            Contains("PlayStation", component.Markup, StringComparison.Ordinal);
            Contains("Games Media", component.Markup, StringComparison.Ordinal);
            Contains("Sacred Symbols 350", component.Markup, StringComparison.Ordinal);
        });

        Equal("Colin Moriarty", BreadcrumbService.Breadcrumbs[^1].Text);
    }

    [Fact]
    public void TopicSortToggle_WhenClicked_RequestsTopicsDescendingThenAscending()
    {
        // Arrange
        RenderProviders();
        int topicRequestCount = 0;
        List<bool> requestedSortDirections = [];

        ClientService.GetPersonByIdHandler = (_, _) => Task.FromResult(Result<Person>.Ok(FrontendTestData.CreatePerson(42, "Colin Moriarty")));
        ClientService.GetPersonDetailsByIdHandler = (_, _) => Task.FromResult(Result<PersonDetails>.Ok(FrontendTestData.CreatePersonDetails()));
        ClientService.GetLatestEpisodeByPersonIdHandler = (_, _) => Task.FromResult(Result<Episode>.None());
        ClientService.GetMostDiscussedTopicsByPersonIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok([]));
        ClientService.GetEpisodesByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<PersonTimelineEntry>>.Ok(new PagedResponse<PersonTimelineEntry>([], 0, 1, 25)));
        ClientService.GetTopicsByPersonIdHandler = (_, pagedRequest, sortDescending, _) =>
        {
            topicRequestCount++;
            requestedSortDirections.Add(sortDescending);
            return Task.FromResult(Result<PagedResponse<Topic>>.Ok(new PagedResponse<Topic>([FrontendTestData.CreateTopic(1, "Alpha")], 1, pagedRequest.PageNumber, pagedRequest.PageSize)));
        };

        var component = RenderComponent<PersonPage>(parameters => parameters.Add(x => x.Id, 42));

        // Act
        component.WaitForAssertion(() => Equal(1, topicRequestCount));
        component.FindAll("button.mud-icon-button").First().Click();

        // Assert
        component.WaitForAssertion(() => Equal(2, topicRequestCount));
        Equal([false, true], requestedSortDirections);
    }

    [Fact]
    public void ViewLatestEpisodeOnPatreon_WhenClicked_InvokesOpenInNewTab()
    {
        // Arrange
        RenderProviders();
        Episode latestEpisode = FrontendTestData.CreateEpisode(88, "Latest Colin Episode", patreonPostLink: "https://patreon.com/posts/88");

        ClientService.GetPersonByIdHandler = (_, _) => Task.FromResult(Result<Person>.Ok(FrontendTestData.CreatePerson(42, "Colin Moriarty")));
        ClientService.GetPersonDetailsByIdHandler = (_, _) => Task.FromResult(Result<PersonDetails>.Ok(FrontendTestData.CreatePersonDetails()));
        ClientService.GetLatestEpisodeByPersonIdHandler = (_, _) => Task.FromResult(Result<Episode>.Ok(latestEpisode));
        ClientService.GetMostDiscussedTopicsByPersonIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok([]));
        ClientService.GetTopicsByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<Topic>>.Ok(new PagedResponse<Topic>([], 0, 1, 50)));
        ClientService.GetEpisodesByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<PersonTimelineEntry>>.Ok(new PagedResponse<PersonTimelineEntry>([], 0, 1, 25)));
        JSInterop.SetupVoid("open", invocation => invocation.Arguments.SequenceEqual(["https://patreon.com/posts/88", "_blank"]));

        var component = RenderComponent<PersonPage>(parameters => parameters.Add(x => x.Id, 42));

        // Act
        component.WaitForAssertion(() => component.FindAll("button").Single(button => button.TextContent.Contains("View on Patreon", StringComparison.Ordinal)).Click());

        // Assert
        JSInterop.VerifyInvoke("open");
    }

    [Fact]
    public void Render_WhenPersonRequestFails_ShowsSnackbarMessage()
    {
        // Arrange
        RenderProviders();
        ClientService.GetPersonByIdHandler = (_, _) => Task.FromResult(Result<Person>.Fail("Person lookup failed."));
        ClientService.GetPersonDetailsByIdHandler = (_, _) => Task.FromResult(Result<PersonDetails>.Ok(FrontendTestData.CreatePersonDetails()));
        ClientService.GetLatestEpisodeByPersonIdHandler = (_, _) => Task.FromResult(Result<Episode>.None());
        ClientService.GetMostDiscussedTopicsByPersonIdHandler = (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.Ok([]));
        ClientService.GetTopicsByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<Topic>>.Ok(new PagedResponse<Topic>([], 0, 1, 50)));
        ClientService.GetEpisodesByPersonIdHandler = (_, _, _, _) => Task.FromResult(Result<PagedResponse<PersonTimelineEntry>>.Ok(new PagedResponse<PersonTimelineEntry>([], 0, 1, 25)));

        // Act
        var component = RenderComponent<PersonPage>(parameters => parameters.Add(x => x.Id, 42));

        // Assert
        component.WaitForAssertion(() => Equal("Person lookup failed.", Snackbar.ShownSnackbars.Single().Message));
    }
}




