using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.DbContext;

/// <summary>
/// The application database context.
/// </summary>
public class LsmArchiveDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LsmArchiveDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public LsmArchiveDbContext(DbContextOptions<LsmArchiveDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LsmArchiveDbContext).Assembly);
    }

    /// <summary>
    /// The Patreon posts.
    /// </summary>
    public DbSet<PatreonPostEntity> PatreonPosts => Set<PatreonPostEntity>();

    /// <summary>
    /// The shows.
    /// </summary>
    public DbSet<ShowEntity> Shows => Set<ShowEntity>();

    /// <summary>
    /// The episodes.
    /// </summary>
    public DbSet<EpisodeEntity> Episodes => Set<EpisodeEntity>();

    /// <summary>
    /// The persons.
    /// </summary>
    public DbSet<PersonEntity> Persons => Set<PersonEntity>();

    /// <summary>
    /// The person-episode relationships.
    /// </summary>
    public DbSet<PersonEpisodeEntity> PersonEpisodes => Set<PersonEpisodeEntity>();

    /// <summary>
    /// The topics.
    /// </summary>
    public DbSet<TopicEntity> Topics => Set<TopicEntity>();

    /// <summary>
    /// The topic-episode relationships.
    /// </summary>
    public DbSet<TopicEpisodeEntity> TopicEpisodes => Set<TopicEpisodeEntity>();

    /// <summary>
    /// The person-topic relationships.
    /// </summary>
    public DbSet<PersonTopicEntity> PersonTopics => Set<PersonTopicEntity>();
}
