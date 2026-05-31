using Microsoft.AspNetCore.Components;

using TheLsmArchive.Web.Frontend.Layout;
using TheLsmArchive.Web.Frontend.Tests.TestSupport;

namespace TheLsmArchive.Web.Frontend.Tests.Layout;

public sealed class MainLayoutTests : FrontendComponentTestContext
{
    [Fact]
    public void Render_WhenAtHomeRoute_HidesBreadcrumbsAndShowsBody()
    {
        // Arrange
        // Act
        var component = RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body, (RenderFragment)(_ => _.AddContent(0, "Home Body"))));

        // Assert
        Contains("The LSM Archive", component.Markup, StringComparison.Ordinal);
        Contains("Home Body", component.Markup, StringComparison.Ordinal);
        DoesNotContain("breadcrumb-nav", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenBreadcrumbsExistAndRouteIsNotHome_ShowsBreadcrumbTrail()
    {
        // Arrange
        NavigationManager.NavigateTo("/Person/42");
        BreadcrumbService.Push("Loading...", "/Person/42");
        BreadcrumbService.ReplaceLast("Colin Moriarty", "/Person/42");

        // Act
        var component = RenderComponent<MainLayout>(parameters =>
            parameters.Add(p => p.Body, (RenderFragment)(_ => _.AddContent(0, "Person Body"))));

        // Assert
        Contains("breadcrumb-nav", component.Markup, StringComparison.Ordinal);
        Contains("Colin Moriarty", component.Markup, StringComparison.Ordinal);
        Contains("Person Body", component.Markup, StringComparison.Ordinal);
    }
}


