using Microsoft.Extensions.Logging;

namespace TheLsmArchive.Domain.Services.Abstractions;

/// <summary>
/// The base class for all database services, providing common functionality and patterns for interacting with the database context.
/// </summary>
public abstract class DatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IDbContextFactory<LsmArchiveDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class with the specified logger and database context factory.
    /// </summary>
    /// <param name="logger">The logger for logging informational messages and errors during database operations.</param>
    /// <param name="dbContextFactory">The database context factory for creating instances of the database context when needed.</param>
    protected DatabaseService(
        ILogger<DatabaseService> logger,
        IDbContextFactory<LsmArchiveDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Executes the specified database operation within a new database context, ensuring proper disposal of the context and handling of exceptions.
    /// </summary>
    /// <param name="operation">The database operation to be executed, represented as a function that takes an instance of the database context and returns a task.</param>
    /// <param name="cancellationToken">The cancellation token to observe while performing the database operation.</param>
    protected async Task ExecuteDatabaseOperation(
        Func<LsmArchiveDbContext, Task> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await operation(dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing a database operation.");
            throw;
        }
    }

    /// <summary>
    /// Executes the specified database operation that returns a result within a new database context, ensuring proper disposal of the context and handling of exceptions.
    /// </summary>
    /// <param name="operation">The database operation to be executed, represented as a function that takes an instance of the database context and returns a task with a result.</param>
    /// <param name="cancellationToken">The cancellation token to observe while performing the database operation.</param>
    /// <typeparam name="T">The type of the result returned by the database operation.</typeparam>
    /// <returns>A task that represents the asynchronous operation, containing the result of the database operation.</returns>
    public async Task<T> ExecuteDatabaseOperation<T>(
        Func<LsmArchiveDbContext, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await using LsmArchiveDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await operation(dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing a database operation.");
            throw;
        }
    }
}
