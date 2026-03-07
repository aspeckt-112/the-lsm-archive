using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using TheLsmArchive.ApiClient;
using TheLsmArchive.ApiClient.Options;
using TheLsmArchive.Web.Frontend;
using TheLsmArchive.Web.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddMudServices()
    .AddScoped<IBreadcrumbService, BreadcrumbService>()
    .AddFrontendOptions()
    .AddApiClientServices();

// If we're not in development, we want to use the same host as the frontend for the API.
builder.Services.PostConfigure<ApiOptions>(options =>
{
    if (!builder.HostEnvironment.IsDevelopment())
    {
        options.BaseUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/') + "/api/";
    }
});

await builder.Build().RunAsync();
