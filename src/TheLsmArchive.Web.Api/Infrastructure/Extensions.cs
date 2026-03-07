using TheLsmArchive.Models.Request.Abstractions;

namespace TheLsmArchive.Web.Api.Infrastructure;

/// <summary>
/// Extension methods for the API project.
/// </summary>
public static class Extensions
{
    extension<T>(IQueryable<T> queryable)
    {
        internal IQueryable<T> WithPaging(PagedRequest pagedRequest)
        {
            int skip = (pagedRequest.PageNumber - 1) * pagedRequest.PageSize;
            return queryable.Skip(skip).Take(pagedRequest.PageSize);
        }
    }
}
