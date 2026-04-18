using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Patreon.Ingestion.Services.Database;

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

    public async Task<int> GetOrCreateAsync(string name, CancellationToken cancellationToken)
    {
        ShowEntity? existingShowEntity = await _dbContext.Shows
            .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name), cancellationToken);

        if (existingShowEntity is not null)
        {
            return existingShowEntity.Id;
        }

        _logger.LogInformation("Show '{ShowName}' not found. Creating new show record.", name);

        ShowEntity newShowEntity = new() { Name = name };

        await _dbContext.Shows.AddAsync(newShowEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return newShowEntity.Id;
    }
}
