using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Web.Api.Features.System;

/// <summary>
/// The implementation of the <see cref="ISystemService"/> interface to provide system-related functionalities.
/// </summary>
public sealed class SystemService : ISystemService
{
    private readonly ILogger<SystemService> _logger;
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public SystemService(
        ILogger<SystemService> logger,
        LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset> GetLastDataSyncDateTimeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting the date and time of the last data synchronization.");

        return _dbContext.PatreonPosts
            .AsNoTracking()
            .OrderByDescending(p => p.Published)
            .Where(p => p.EpisodeId != null)
            .Select(p => p.Published)
            .FirstAsync(cancellationToken);
    }
}
