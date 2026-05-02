using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Helpers;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

/// <summary>
/// The service responsible for resolving and creating topic records with deduplication.
/// </summary>
public sealed class TopicService
{
    private readonly ILogger<TopicService> _logger;
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public TopicService(ILogger<TopicService> logger, LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the existing <see cref="TopicEntity"/> matching the given name, or creates and tracks a new one.
    /// Matching first attempts an exact normalized-key lookup, then falls back to Postgres trigram similarity (threshold 0.8).
    /// The caller is responsible for calling <c>SaveChangesAsync</c> to persist any newly created entity.
    /// </summary>
    /// <param name="name">The topic's display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The existing or newly created <see cref="TopicEntity"/>.</returns>
    public async Task<TopicEntity> GetOrCreateAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        // 1. Exact match via canonical normalized key.
        TopicEntity? topic = await _dbContext.Topics
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName, cancellationToken);

        if (topic is not null)
        {
            _logger.LogInformation(
                "Topic '{TopicName}' already exists with ID {TopicId} (normalized key: {NormalizedName})",
                name, topic.Id, normalizedName);

            return topic;
        }

        // 2. Fuzzy match via Postgres trigram similarity.
        // A threshold of 0.8 ensures high similarity while preventing distinct topics like
        // "Game Name" and "Game Name Remaster" from being merged.
        topic = await _dbContext.Topics
            .Where(t => EF.Functions.TrigramsSimilarity(t.Name, name) > 0.8)
            .OrderByDescending(t => EF.Functions.TrigramsSimilarity(t.Name, name))
            .FirstOrDefaultAsync(cancellationToken);

        if (topic is not null)
        {
            _logger.LogInformation(
                "Fuzzy matched topic '{InputName}' to existing topic '{MatchedName}' (ID {TopicId})",
                name, topic.Name, topic.Id);

            return topic;
        }

        topic = new TopicEntity { Name = name, NormalizedName = normalizedName };
        _dbContext.Topics.Add(topic);

        return topic;
    }
}
