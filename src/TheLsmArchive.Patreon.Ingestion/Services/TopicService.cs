using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Services.Abstractions;
using TheLsmArchive.Patreon.Ingestion.Helpers;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for resolving and creating topic records with deduplication.
/// </summary>
public sealed class TopicService : DatabaseService
{
    private readonly ILogger<TopicService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public TopicService(ILogger<TopicService> logger, IDbContextFactory<LsmArchiveDbContext> dbContextFactory) : base(
        logger, dbContextFactory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the ID of the existing topic matching the given name, or creates a new topic record and returns its ID.
    /// Matching first attempts an exact normalized-key lookup, then falls back to Postgres trigram similarity (threshold 0.8).
    /// </summary>
    /// <param name="name">The topic's display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the existing or newly created topic.</returns>
    public Task<int> GetOrCreateAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        return ExecuteDatabaseOperation(async dbContext =>
        {
            // 1. Exact match via canonical normalized key.
            TopicEntity? topic = await dbContext.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName, cancellationToken);

            if (topic is not null)
            {
                _logger.LogInformation(
                    "Topic '{TopicName}' already exists with ID {TopicId} (normalized key: {NormalizedName})",
                    name, topic.Id, normalizedName);

                return topic.Id;
            }

            // 2. Fuzzy match via Postgres trigram similarity.
            // A threshold of 0.8 ensures high similarity while preventing distinct topics like
            // "Game Name" and "Game Name Remaster" from being merged.
            topic = await dbContext.Topics
                .AsNoTracking()
                .Where(t => EF.Functions.TrigramsSimilarity(t.Name, name) > 0.8)
                .OrderByDescending(t => EF.Functions.TrigramsSimilarity(t.Name, name))
                .FirstOrDefaultAsync(cancellationToken);

            if (topic is not null)
            {
                _logger.LogInformation(
                    "Fuzzy matched topic '{InputName}' to existing topic '{MatchedName}' (ID {TopicId})",
                    name, topic.Name, topic.Id);

                return topic.Id;
            }

            topic = new TopicEntity { Name = name, NormalizedName = normalizedName };
            dbContext.Topics.Add(topic);
            await dbContext.SaveChangesAsync(cancellationToken);

            return topic.Id;
        }, cancellationToken);
    }
}
