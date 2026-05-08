using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Services.Abstractions;
using TheLsmArchive.Patreon.Ingestion.Helpers;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The service responsible for resolving and creating person records with deduplication.
/// </summary>
public sealed class PersonService : DatabaseService
{
    private readonly ILogger<PersonService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public PersonService(
        ILogger<PersonService> logger,
        IDbContextFactory<LsmArchiveDbContext> dbContextFactory) : base(logger, dbContextFactory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the ID of the existing person matching the given name, or creates a new person record and returns its ID.
    /// Matching first attempts an exact normalized-key lookup, then falls back to Postgres trigram similarity (threshold 0.8).
    /// </summary>
    /// <param name="name">The person's display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the existing or newly created person.</returns>
    public Task<int> GetOrCreateAsync(string name, CancellationToken cancellationToken)
    {
        name = name.Trim();
        string normalizedName = LookupKeyNormalizer.Normalize(name);

        return ExecuteDatabaseOperation(async dbContext =>
        {
            // 1. Exact match via canonical normalized key.
            PersonEntity? person = await dbContext.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.NormalizedName == normalizedName, cancellationToken);

            if (person is not null)
            {
                _logger.LogInformation(
                    "Person '{Person}' already exists with ID {PersonId} (normalized key: {NormalizedName})",
                    name, person.Id, normalizedName);

                return person.Id;
            }

            // 2. Fuzzy match via Postgres trigram similarity.
            person = await dbContext.Persons
                .AsNoTracking()
                .Where(p => EF.Functions.TrigramsSimilarity(p.Name, name) > 0.8)
                .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, name))
                .FirstOrDefaultAsync(cancellationToken);

            if (person is not null)
            {
                _logger.LogInformation(
                    "Fuzzy matched person '{InputName}' to existing person '{MatchedName}' (ID {PersonId})",
                    name, person.Name, person.Id);

                return person.Id;
            }

            person = new PersonEntity { Name = name, NormalizedName = normalizedName };
            dbContext.Persons.Add(person);
            await dbContext.SaveChangesAsync(cancellationToken);

            return person.Id;
        }, cancellationToken);
    }
}
