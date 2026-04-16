using System.Linq.Expressions;

using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Web.Api.Features.Persons;

/// <summary>
/// The person service.
/// </summary>
public sealed class PersonService : IPersonService
{
    private readonly ILogger<PersonService> _logger;

    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public PersonService(
        ILogger<PersonService> logger,
        LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<Person?> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting person with ID: {Id}", id);

        Expression<Func<PersonEntity, Person>> mapToPerson =
            mapToPerson => new Person(
                Id: mapToPerson.Id,
                Name: mapToPerson.Name);

        return _dbContext.Persons
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(mapToPerson)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public async Task<PersonDetails?> GetDetailsById(int id, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting details for person with ID: {Id}", id);

        var result = await _dbContext.Persons
        .AsNoTracking()
        .Include(p => p.PersonEpisodes)
        .ThenInclude(pe => pe.Episode)
        .Where(p => p.Id == id)
        .Select(p => new
        {
            FirstAppearedUtc = p.PersonEpisodes
                .Min(pe => pe.Episode.ReleaseDateUtc),
            LastAppearedUtc = p.PersonEpisodes.Max(pe => pe.Episode.ReleaseDateUtc)
        })
        .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? null
            : new PersonDetails(
            FirstAppeared: DateOnly.FromDateTime(result.FirstAppearedUtc.DateTime),
            LastAppeared: DateOnly.FromDateTime(result.LastAppearedUtc.DateTime));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="id"/> is negative or zero.</exception>
    public Task<List<Person>> GetByEpisodeId(
        int id,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        _logger.LogInformation("Getting people for episode with ID: {Id}", id);

        Expression<Func<PersonEpisodeEntity, Person>> mapToPerson =
            mapToPerson => new Person(
                Id: mapToPerson.Person.Id,
                Name: mapToPerson.Person.Name);

        return _dbContext.PersonEpisodes
            .AsNoTracking()
            .Include(pe => pe.Person)
            .Where(pe => pe.EpisodeId == id)
            .OrderBy(pe => pe.Person.Name)
            .Select(mapToPerson)
            .ToListAsync(cancellationToken);
    }
}
