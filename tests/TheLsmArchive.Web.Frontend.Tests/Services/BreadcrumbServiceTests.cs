using MudBlazor;

using TheLsmArchive.Web.Frontend.Services;

namespace TheLsmArchive.Web.Frontend.Tests.Services;

public sealed class BreadcrumbServiceTests
{
    [Fact]
    public void Constructor_InitializesBreadcrumbsToHome()
    {
        // Arrange & Act
        BreadcrumbService service = new();

        // Assert
        Equal([new BreadcrumbItem("Home", "/", icon: Icons.Material.Filled.Home)], service.Breadcrumbs);
    }

    [Fact]
    public void Push_WhenNavigatingHome_ResetsBreadcrumbs()
    {
        // Arrange
        BreadcrumbService service = new();
        service.Push("People", "/people", Icons.Material.Filled.People);

        // Act
        service.Push("Home", "/", Icons.Material.Filled.Home);

        // Assert
        Equal([new BreadcrumbItem("Home", "/", icon: Icons.Material.Filled.Home)], service.Breadcrumbs);
    }

    [Fact]
    public void Push_WhenHrefAlreadyExists_TruncatesBreadcrumbTrail()
    {
        // Arrange
        BreadcrumbService service = new();
        service.Push("People", "/people", Icons.Material.Filled.People);
        service.Push("Colin Moriarty", "/person/1", Icons.Material.Filled.Person);
        service.Push("PlayStation", "/topic/7", Icons.Material.Filled.Tag);

        // Act
        service.Push("People", "/people", Icons.Material.Filled.People);

        // Assert
        Equal(
            [
                new BreadcrumbItem("Home", "/", icon: Icons.Material.Filled.Home),
                new BreadcrumbItem("People", "/people", icon: Icons.Material.Filled.People)
            ],
            service.Breadcrumbs);
    }

    [Fact]
    public void ReplaceLast_WithTextOnly_UpdatesLastBreadcrumbTextAndPreservesHref()
    {
        // Arrange
        BreadcrumbService service = new();
        service.Push("Loading...", "/person/1", Icons.Material.Filled.Person);

        // Act
        service.ReplaceLast("Colin Moriarty");

        // Assert
        Equal(
            new BreadcrumbItem("Colin Moriarty", "/person/1", icon: Icons.Material.Filled.Person),
            service.Breadcrumbs[^1]);
    }

    [Fact]
    public void ReplaceLast_WithTextAndHref_UpdatesLastBreadcrumb()
    {
        // Arrange
        BreadcrumbService service = new();
        service.Push("Loading...", "/person/loading", Icons.Material.Filled.Person);

        // Act
        service.ReplaceLast("Colin Moriarty", "/person/1", Icons.Material.Filled.Person);

        // Assert
        Equal(
            new BreadcrumbItem("Colin Moriarty", "/person/1", icon: Icons.Material.Filled.Person),
            service.Breadcrumbs[^1]);
    }

    [Fact]
    public void Reset_RaisesBreadcrumbsChangedEvent()
    {
        // Arrange
        BreadcrumbService service = new();
        int eventCount = 0;
        service.OnBreadcrumbsChanged += (_, _) => eventCount++;

        // Act
        service.Reset();

        // Assert
        Equal(1, eventCount);
    }
}

