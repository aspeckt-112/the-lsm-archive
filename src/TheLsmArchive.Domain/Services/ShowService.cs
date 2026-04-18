using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Models;

namespace TheLsmArchive.Domain.Services;

/// <summary>
/// The show service for interacting with show data in the database.
/// </summary>
public sealed class ShowService
{
    private readonly ILogger<ShowService> _logger;
    private readonly LsmArchiveDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContext">The database context.</param>
    public ShowService(
        ILogger<ShowService> logger,
        LsmArchiveDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the show by name or creates a new show if it does not exist, and returns the show information.
    /// </summary>
    /// <param name="name">The name of the show.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>>The show information.</returns>
    public async Task<Show> GetOrCreateAsync(
        string name,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to get or create show with name '{ShowName}'.", name);

        ShowEntity? existingShowEntity = await _dbContext.Shows
            .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name), cancellationToken);

        if (existingShowEntity is not null)
        {
            _logger.LogInformation("Show '{ShowName}' found with ID {ShowId}.", name, existingShowEntity.Id);
            return new Show(existingShowEntity.Id, existingShowEntity.Name);
        }

        _logger.LogInformation("Show '{ShowName}' not found. Creating new show record.", name);

        ShowEntity newShowEntity = new() { Name = name };

        await _dbContext.Shows.AddAsync(newShowEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Show '{ShowName}' created with ID {ShowId}.", name, newShowEntity.Id);

        return new Show(newShowEntity.Id, newShowEntity.Name);
    }
}
