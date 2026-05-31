using Bunit;
using Bunit.JSInterop;
using BunitTestContext = Bunit.TestContext;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using MudBlazor.Services;

using TheLsmArchive.ApiClient.Services.Abstractions;
using TheLsmArchive.Web.Frontend.Services;

namespace TheLsmArchive.Web.Frontend.Tests.TestSupport;

/// <summary>
/// Provides a shared bUnit test context configuration for frontend component tests.
/// </summary>
public abstract class FrontendTestContext : BunitTestContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrontendTestContext"/> class.
    /// </summary>
    protected FrontendTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<int>("mudpopoverHelper.countProviders").SetResult(1);

        Services.AddMudServices();
        Services.AddSingleton<IBreadcrumbService, BreadcrumbService>();
        Services.AddSingleton<NavigationManager, FakeNavigationManager>();
        Services.AddSingleton(CreateClientService());
        Services.AddSingleton<IJSRuntime>(JSInterop.JSRuntime);

        Services.AddLogging();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    /// <summary>
    /// Creates the client service instance used by the test context.
    /// </summary>
    /// <returns>The configured client service mock.</returns>
    protected abstract ILsmArchiveClientService CreateClientService();
}




