using Bunit;

using TheLsmArchive.Web.Frontend.Components;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Components;

public sealed class PersonCardTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenPersonProvided_ShowsNameAndNavigatesOnClick()
    {
        // Arrange
        var person = FrontendTestData.CreatePerson(42, "Matty Plays");

        // Act
        var component = RenderComponent<PersonCard>(parameters =>
            parameters.Add(x => x.Person, person));

        component.Find(".mud-card-content").Click();

        // Assert
        Contains("Matty Plays", component.Markup, StringComparison.Ordinal);
        Equal("https://localhost/Person/42", NavigationManager.Uri);
    }
}


