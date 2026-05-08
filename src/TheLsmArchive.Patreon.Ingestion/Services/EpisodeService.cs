using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for creating and retrieving episode records linked to Patreon posts.
/// </summary>
public sealed class EpisodeService : DatabaseService
{
    private readonly ILogger<EpisodeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public EpisodeService(
        ILogger<EpisodeService> logger,
        IDbContextFactory<LsmArchiveDbContext> dbContextFactory) :
        base(logger, dbContextFactory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the ID of the existing episode for the given post, or creates a new episode record and returns its ID.
    /// </summary>
    /// <param name="post">The Patreon post entity the episode is linked to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the existing or newly created episode.</returns>
    public Task<int> GetOrCreateAsync(
        PatreonPostEntity post,
        CancellationToken cancellationToken)
    {
        return ExecuteDatabaseOperation(async dbContext =>
        {
            EpisodeEntity? existingEpisode = await dbContext.Episodes
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

            dbContext.Episodes.Add(episode);
            await dbContext.SaveChangesAsync(cancellationToken);

            return episode.Id;
        }, cancellationToken);
    }
}
