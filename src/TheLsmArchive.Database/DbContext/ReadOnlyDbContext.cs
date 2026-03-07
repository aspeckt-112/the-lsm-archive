using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext.Abstractions;

namespace TheLsmArchive.Database.DbContext;

public class ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options) : BaseDbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
