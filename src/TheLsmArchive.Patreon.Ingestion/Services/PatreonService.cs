using System.Collections.Immutable;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Services.Abstractions;
using TheLsmArchive.Patreon.Ingestion.Models;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The Patreon service for interacting with Patreon data in the database.
/// </summary>
public sealed class PatreonService : DatabaseService
{
    private readonly ILogger<PatreonService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public PatreonService(
        ILogger<PatreonService> logger,
        IDbContextFactory<LsmArchiveDbContext> dbContextFactory) : base(logger, dbContextFactory)
    {
        _logger = logger;
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

        await ExecuteDatabaseOperation(async dbContext =>
        {
            HashSet<int> existingPostIds = await dbContext.PatreonPosts
                .AsNoTracking()
                .Where(p => p.ShowId == showId)
                .Select(p => p.PatreonId)
                .ToHashSetAsync(cancellationToken);

            foreach (PatreonPost post in feed.Posts)
            {
                if (existingPostIds.Contains(post.Id))
                {
                    _logger.LogInformation(
                        "Post with Patreon ID {PatreonId} already exists for show ID {ShowId}, skipping.", post.Id,
                        showId);
                    continue;
                }

                PatreonPostEntity postEntity = post.ToEntity(showId);
                await dbContext.PatreonPosts.AddAsync(postEntity, cancellationToken);
                _logger.LogInformation("Added new post with Patreon ID {PatreonId} for show ID {ShowId}.", post.Id,
                    showId);
            }

            _logger.LogInformation("Saving changes to the database for feed '{FeedTitle}' and show ID {ShowId}.",
                feed.Title, showId);
            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Finished ingesting feed '{FeedTitle}' for show ID {ShowId}.", feed.Title, showId);
    }

    public async Task<ImmutableList<PendingPost>> GetPendingPosts(
        int showId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving pending posts for show ID {ShowId}.", showId);

        await ThrowIfShowDoesNotExist(showId, cancellationToken);

        List<PendingPost> pendingPosts = await ExecuteDatabaseOperation(async dbContext =>
        {
            return await dbContext.PatreonPosts
                .AsNoTracking()
                .Where(p => p.ShowId == showId && (p.ProcessingError != null || p.EpisodeId == null))
                .Select(p => new PendingPost(p.Id, p.Title, p.ProcessingError))
                .ToListAsync(cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Retrieved {Count} pending posts for show ID {ShowId}.", pendingPosts.Count, showId);

        return [.. pendingPosts];
    }

    /// <summary>
    /// Marks the given post as processed by setting the EpisodeId and clearing any existing processing error message, indicating that the post has been successfully processed and associated with an episode in the database.
    /// </summary>
    /// <param name="postId">The database ID of the Patreon post.</param>
    /// <param name="episodeId">The ID of the episode that was created for the post.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task MarkPostAsProcessedAsync(int postId, int episodeId, CancellationToken cancellationToken)
    {
        return ExecuteDatabaseOperation(async dbContext =>
        {
            PatreonPostEntity postEntity = await dbContext.PatreonPosts
                                               .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken)
                                           ?? throw new InvalidOperationException(
                                               $"Patreon post with ID {postId} was not found.");

            postEntity.EpisodeId = episodeId;
            postEntity.ProcessingError = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Persists a processing error message on the given post for retry on the next ingestion cycle.
    /// </summary>
    /// <param name="postId">The database ID of the Patreon post.</param>
    /// <param name="errorMessage">The error message to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task SaveProcessingErrorAsync(int postId, string errorMessage, CancellationToken cancellationToken)
    {
        return ExecuteDatabaseOperation(async dbContext =>
        {
            PatreonPostEntity postEntity = await dbContext.PatreonPosts
                                               .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken)
                                           ?? throw new InvalidOperationException(
                                               $"Patreon post with ID {postId} was not found.");

            postEntity.ProcessingError = errorMessage;

            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private async Task ThrowIfShowDoesNotExist(int showId, CancellationToken cancellationToken)
    {
        bool showExists = await ExecuteDatabaseOperation(async dbContext =>
        {
            return await dbContext.Shows
                .AsNoTracking()
                .AnyAsync(s => s.Id == showId, cancellationToken);
        }, cancellationToken);

        if (!showExists)
        {
            _logger.LogError("Show with ID {ShowId} does not exist in the database.", showId);
            throw new InvalidOperationException($"Show with ID {showId} does not exist in the database.");
        }
    }
}
