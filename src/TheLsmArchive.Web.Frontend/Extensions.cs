using Microsoft.JSInterop;

using TheLsmArchive.ApiClient.Options;

namespace TheLsmArchive.Web.Frontend;

internal static class Extensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddFrontendOptions()
        {
            services.AddOptionsWithValidateOnStart<ApiOptions>()
                .BindConfiguration(nameof(ApiOptions));

            return services;
        }
    }

    extension(IJSRuntime jsRuntime)
    {
        internal Task OpenInNewTab(string url) => jsRuntime.InvokeVoidAsync("open", url, "_blank").AsTask();
    }
}
