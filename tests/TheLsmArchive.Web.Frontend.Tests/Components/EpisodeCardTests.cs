using Bunit;

using TheLsmArchive.Web.Frontend.Components;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Components;

public sealed class EpisodeCardTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenEpisodeProvided_ShowsTitleAndNavigatesOnClick()
    {
        // Arrange
        var episode = FrontendTestData.CreateEpisode(99, "Defining Duke 200");

        // Act
        var component = RenderComponent<EpisodeCard>(parameters =>
            parameters.Add(x => x.Episode, episode));

        component.Find(".mud-card-content").Click();

        // Assert
        Contains("Defining Duke 200", component.Markup, StringComparison.Ordinal);
        Equal("https://localhost/Episode/99", NavigationManager.Uri);
    }
}


