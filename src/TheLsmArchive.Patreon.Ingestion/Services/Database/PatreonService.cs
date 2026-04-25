using System.Collections.Immutable;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

/// <summary>
/// The Patreon service for interacting with Patreon data in the database.
/// </summary>
public sealed class PatreonService
{
    private readonly ILogger<PatreonService> _logger;
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public PatreonService(
        ILogger<PatreonService> logger,
        LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Ingests the given Patreon feed for the specified show ID, adding new posts to the database while avoiding duplicates based on Patreon post IDs.
    /// </summary>
    /// <param name="showId">The ID of the show associated with the Patreon feed.</param>
    /// <param name="feed">The Patreon feed to be ingested, containing the feed title and a list of posts.</param>
    /// <param name="cancellationToken">The cancellation token to observe while performing the ingestion operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task IngestFeed(
        int showId,
        PatreonFeed feed,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ingesting feed '{FeedTitle}' for show ID {ShowId}", feed.Title, showId);

        await ThrowIfShowDoesNotExist(showId, cancellationToken);

        HashSet<int> existingPostIds = await _dbContext.PatreonPosts
            .AsNoTracking()
            .Where(p => p.ShowId == showId)
            .Select(p => p.PatreonId)
            .ToHashSetAsync(cancellationToken);

        foreach (PatreonPost post in feed.Posts)
        {
            if (existingPostIds.Contains(post.Id))
            {
                _logger.LogInformation("Post with Patreon ID {PatreonId} already exists for show ID {ShowId}, skipping.", post.Id, showId);
                continue;
            }

            PatreonPostEntity postEntity = post.ToEntity(showId);
            await _dbContext.PatreonPosts.AddAsync(postEntity, cancellationToken);
            _logger.LogInformation("Added new post with Patreon ID {PatreonId} for show ID {ShowId}.", post.Id, showId);
        }

        _logger.LogInformation("Saving changes to the database for feed '{FeedTitle}' and show ID {ShowId}.", feed.Title, showId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Finished ingesting feed '{FeedTitle}' for show ID {ShowId}.", feed.Title, showId);
    }

    public async Task<ImmutableList<PendingPost>> GetPendingPosts(
        int showId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving pending posts for show ID {ShowId}.", showId);

        await ThrowIfShowDoesNotExist(showId, cancellationToken);

        List<PendingPost> pendingPosts = await _dbContext.PatreonPosts
            .AsNoTracking()
            .Where(p => p.ShowId == showId && (p.ProcessingError != null || p.EpisodeId == null))
            .Select(p => new PendingPost(p.PatreonId, p.Title, p.ProcessingError))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} pending posts for show ID {ShowId}.", pendingPosts.Count, showId);
        return [.. pendingPosts];
    }

    private async Task ThrowIfShowDoesNotExist(int showId, CancellationToken cancellationToken)
    {
        bool showExists = await _dbContext.Shows
            .AsNoTracking()
            .AnyAsync(s => s.Id == showId, cancellationToken);

        if (!showExists)
        {
            _logger.LogError("Show with ID {ShowId} does not exist in the database.", showId);
            throw new InvalidOperationException($"Show with ID {showId} does not exist in the database.");
        }
    }
}