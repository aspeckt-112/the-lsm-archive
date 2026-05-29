using Microsoft.AspNetCore.Components;

namespace TheLsmArchive.Web.Frontend.Tests.TestSupport;

/// <summary>
/// A simple navigation manager for component tests.
/// </summary>
public sealed class FakeNavigationManager : NavigationManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FakeNavigationManager"/> class.
    /// </summary>
    public FakeNavigationManager()
    {
        Initialize("https://localhost/", "https://localhost/");
    }

    /// <inheritdoc />
    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        Uri = ToAbsoluteUri(uri).ToString();
    }
}

