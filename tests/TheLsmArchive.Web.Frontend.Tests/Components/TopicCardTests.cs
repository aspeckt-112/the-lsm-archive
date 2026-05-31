using Bunit;

using TheLsmArchive.Web.Frontend.Components;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Components;

public sealed class TopicCardTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenTopicProvided_ShowsNameAndNavigatesOnClick()
    {
        // Arrange
        var topic = FrontendTestData.CreateTopic(7, "Games Media");

        // Act
        var component = RenderComponent<TopicCard>(parameters =>
            parameters.Add(x => x.Topic, topic));

        component.Find(".mud-card-content").Click();

        // Assert
        Contains("Games Media", component.Markup, StringComparison.Ordinal);
        Equal("https://localhost/Topic/7", NavigationManager.Uri);
    }
}


