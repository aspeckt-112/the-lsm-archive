using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TheLsmArchive.ApiClient.Options;
using TheLsmArchive.ApiClient.Services;
using TheLsmArchive.ApiClient.Services.Abstractions;
using TheLsmArchive.Models.Request;

namespace TheLsmArchive.ApiClient;

/// <summary>
/// Extensions for the API client.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Extensions for IServiceCollection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiClientServices()
        {
            services.AddHttpClient<ILsmArchiveClientService, LsmArchiveClientService>((sp, client) =>
            {
                ApiOptions options = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            });

            return services;
        }
    }

    /// <summary>
    /// Extensions for SearchRequest.
    /// </summary>
    /// <param name="searchRequest"></param>
    extension(SearchRequest searchRequest)
    {
        internal string ToQueryString()
        {
            StringBuilder builder = new("?");

            if (!string.IsNullOrWhiteSpace(searchRequest.SearchTerm))
            {
                builder.Append($"searchTerm={Uri.EscapeDataString(searchRequest.SearchTerm)}&");
            }

            builder.Append($"entityType={searchRequest.EntityType}&");
            builder.Append($"pageNumber={searchRequest.PageNumber}&");
            builder.Append($"pageSize={searchRequest.PageSize}");

            return builder.ToString();
        }
    }

    /// <summary>
    /// Extensions for PagedItemRequest.
    /// </summary>
    /// <param name="pagedItemRequest"></param>
    extension(PagedItemRequest pagedItemRequest)
    {
        internal string ToQueryString()
        {
            string query = $"?pageNumber={pagedItemRequest.PageNumber}&pageSize={pagedItemRequest.PageSize}";

            if (!string.IsNullOrWhiteSpace(pagedItemRequest.SearchTerm))
            {
                query += $"&searchTerm={Uri.EscapeDataString(pagedItemRequest.SearchTerm)}";
            }

            return query;
        }
    }
}
