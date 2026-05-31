using Bunit;

using Microsoft.JSInterop;

using TheLsmArchive.ApiClient;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Frontend.Pages;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Pages;

public sealed class EpisodePageTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenEpisodeAndRelationsLoad_ShowsEpisodePeopleAndTopics()
    {
        // Arrange
        RenderProviders();
        Episode episode = FrontendTestData.CreateEpisode(9, "Sacred Symbols 350", summaryHtml: "<p>Big episode summary.</p>");
        List<Person> people =
        [
            FrontendTestData.CreatePerson(1, "Colin Moriarty"),
            FrontendTestData.CreatePerson(2, "Chris Ray Gun")
        ];
        List<Topic> topics =
        [
            FrontendTestData.CreateTopic(3, "PlayStation"),
            FrontendTestData.CreateTopic(4, "Xbox")
        ];

        ClientService.GetEpisodeByIdHandler = (_, _) => Task.FromResult(Result<Episode>.Ok(episode));
        ClientService.GetPersonsByEpisodeIdHandler = (_, _) => Task.FromResult(Result<List<Person>>.Ok(people));
        ClientService.GetTopicsByEpisodeIdHandler = (_, _) => Task.FromResult(Result<List<Topic>>.Ok(topics));

        // Act
        var component = RenderComponent<EpisodePage>(parameters => parameters.Add(x => x.Id, 9));

        // Assert
        component.WaitForAssertion(() =>
        {
            Contains("Sacred Symbols 350", component.Markup, StringComparison.Ordinal);
            Contains("Big episode summary.", component.Markup, StringComparison.Ordinal);
            Contains("Colin Moriarty", component.Markup, StringComparison.Ordinal);
            Contains("Chris Ray Gun", component.Markup, StringComparison.Ordinal);
            Contains("PlayStation", component.Markup, StringComparison.Ordinal);
            Contains("Xbox", component.Markup, StringComparison.Ordinal);
        });

        Equal("Sacred Symbols 350", BreadcrumbService.Breadcrumbs[^1].Text);
    }

    [Fact]
    public void ViewOnPatreon_WhenClicked_InvokesOpenInNewTab()
    {
        // Arrange
        RenderProviders();
        Episode episode = FrontendTestData.CreateEpisode(9, "Sacred Symbols 350", patreonPostLink: "https://patreon.com/posts/9");

        ClientService.GetEpisodeByIdHandler = (_, _) => Task.FromResult(Result<Episode>.Ok(episode));
        ClientService.GetPersonsByEpisodeIdHandler = (_, _) => Task.FromResult(Result<List<Person>>.Ok([]));
        ClientService.GetTopicsByEpisodeIdHandler = (_, _) => Task.FromResult(Result<List<Topic>>.Ok([]));
        JSInterop.SetupVoid("open", invocation => invocation.Arguments.SequenceEqual(["https://patreon.com/posts/9", "_blank"]));

        var component = RenderComponent<EpisodePage>(parameters => parameters.Add(x => x.Id, 9));

        // Act
        component.WaitForAssertion(() => component.FindAll("button").Single(button => button.TextContent.Contains("View on Patreon", StringComparison.Ordinal)).Click());

        // Assert
        JSInterop.VerifyInvoke("open");
    }

    [Fact]
    public void Render_WhenEpisodeRequestFails_ShowsSnackbarMessage()
    {
        // Arrange
        RenderProviders();
        ClientService.GetEpisodeByIdHandler = (_, _) => Task.FromResult(Result<Episode>.Fail("Episode lookup failed."));
        ClientService.GetPersonsByEpisodeIdHandler = (_, _) => Task.FromResult(Result<List<Person>>.Ok([]));
        ClientService.GetTopicsByEpisodeIdHandler = (_, _) => Task.FromResult(Result<List<Topic>>.Ok([]));

        // Act
        var component = RenderComponent<EpisodePage>(parameters => parameters.Add(x => x.Id, 99));

        // Assert
        component.WaitForAssertion(() => Equal("Episode lookup failed.", Snackbar.ShownSnackbars.Single().Message));
    }
}




