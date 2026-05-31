using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

/// <summary>
/// Shared test data helper for creating show entities.
/// </summary>
internal static class ShowTestDataHelper
{
    /// <summary>
    /// Creates and persists a show entity for integration tests.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="showName">The show name.</param>
    /// <returns>The persisted show entity.</returns>
    internal static async Task<ShowEntity> CreateShowAsync(
        LsmArchiveDbContext dbContext,
        CancellationToken cancellationToken,
        string showName = "Test Show")
    {
        ShowEntity show = new() { Name = showName };
        await dbContext.Shows.AddAsync(show, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return show;
    }
}
