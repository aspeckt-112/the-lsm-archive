using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using MudBlazor;

using TheLsmArchive.ApiClient.Services.Abstractions;
using TheLsmArchive.Web.Frontend.Services;

namespace TheLsmArchive.Web.Frontend.Tests.TestSupport;

/// <summary>
/// Provides common frontend test dependencies and helpers.
/// </summary>
public abstract class FrontendComponentTestContext : FrontendTestContext
{
    /// <summary>
    /// Gets the configurable client service mock.
    /// </summary>
    protected MockLsmArchiveClientService ClientService { get; } = new();

    /// <summary>
    /// Gets the breadcrumb service.
    /// </summary>
    protected IBreadcrumbService BreadcrumbService => Services.GetRequiredService<IBreadcrumbService>();

    /// <summary>
    /// Gets the navigation manager.
    /// </summary>
    protected FakeNavigationManager NavigationManager => (FakeNavigationManager)Services.GetRequiredService<NavigationManager>();

    /// <summary>
    /// Gets the snackbar service.
    /// </summary>
    protected ISnackbar Snackbar => Services.GetRequiredService<ISnackbar>();

    /// <inheritdoc />
    protected override ILsmArchiveClientService CreateClientService() => ClientService;

    /// <summary>
    /// Renders the shared MudBlazor providers needed by page components.
    /// </summary>
    protected void RenderProviders()
    {
        RenderComponent<MudPopoverProvider>();
        RenderComponent<MudDialogProvider>();
        RenderComponent<MudSnackbarProvider>();
    }
}



