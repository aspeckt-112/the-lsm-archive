using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

/// <summary>
/// The service responsible for creating and retrieving episode records linked to Patreon posts.
/// </summary>
public sealed class EpisodeService
{
    private readonly ILogger<EpisodeService> _logger;
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public EpisodeService(ILogger<EpisodeService> logger, LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the ID of the existing episode for the given post, or creates a new episode record and returns its ID.
    /// </summary>
    /// <param name="post">The Patreon post entity the episode is linked to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the existing or newly created episode.</returns>
    public async Task<int> GetOrCreateAsync(
        PatreonPostEntity post,
        CancellationToken cancellationToken)
    {
        EpisodeEntity? existingEpisode = await _dbContext.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        if (existingEpisode is not null)
        {
            _logger.LogInformation(
                "Episode for post '{PostTitle}' already exists with ID {EpisodeId}",
                post.Title,
                existingEpisode.Id);

            return existingEpisode.Id;
        }

        var episode = new EpisodeEntity
        {
            ShowId = post.ShowId,
            Title = post.Title,
            ReleaseDateUtc = post.Published.UtcDateTime,
            PatreonPostId = post.Id
        };

        _dbContext.Episodes.Add(episode);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return episode.Id;
    }
}
