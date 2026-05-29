using MudBlazor;

using TheLsmArchive.Web.Frontend.Components;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Components;

public sealed class SectionHeaderTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenParametersProvided_ShowsTitleAndIcon()
    {
        // Act
        var component = RenderComponent<SectionHeader>(parameters =>
            parameters
                .Add(x => x.Title, "Recent Episodes")
                .Add(x => x.Icon, Icons.Material.Filled.NewReleases)
                .Add(x => x.Color, Color.Primary));

        // Assert
        Contains("Recent Episodes", component.Markup, StringComparison.Ordinal);
        Contains(Icons.Material.Filled.NewReleases, component.Markup, StringComparison.Ordinal);
    }
}

