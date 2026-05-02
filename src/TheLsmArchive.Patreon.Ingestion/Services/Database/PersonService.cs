using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Helpers;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

/// <summary>
/// The service responsible for resolving and creating person records with deduplication.
/// </summary>
public sealed class PersonService
{
    private readonly ILogger<PersonService> _logger;
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public PersonService(ILogger<PersonService> logger, LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the existing <see cref="PersonEntity"/> matching the given name, or creates and tracks a new one.
    /// Matching first attempts an exact normalized-key lookup, then falls back to Postgres trigram similarity (threshold 0.8).
    /// The caller is responsible for calling <c>SaveChangesAsync</c> to persist any newly created entity.
    /// </summary>
    /// <param name="name">The person's display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The existing or newly created <see cref="PersonEntity"/>.</returns>
    public async Task<PersonEntity> GetOrCreateAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        // 1. Exact match via canonical normalized key.
        PersonEntity? person = await _dbContext.Persons
            .FirstOrDefaultAsync(p => p.NormalizedName == normalizedName, cancellationToken);

        if (person is not null)
        {
            _logger.LogInformation(
                "Person '{Person}' already exists with ID {PersonId} (normalized key: {NormalizedName})",
                name, person.Id, normalizedName);

            return person;
        }

        // 2. Fuzzy match via Postgres trigram similarity.
        person = await _dbContext.Persons
            .Where(p => EF.Functions.TrigramsSimilarity(p.Name, name) > 0.8)
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, name))
            .FirstOrDefaultAsync(cancellationToken);

        if (person is not null)
        {
            _logger.LogInformation(
                "Fuzzy matched person '{InputName}' to existing person '{MatchedName}' (ID {PersonId})",
                name, person.Name, person.Id);

            return person;
        }

        person = new PersonEntity { Name = name, NormalizedName = normalizedName };
        _dbContext.Persons.Add(person);

        return person;
    }
}
