using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext.Abstractions;

namespace TheLsmArchive.Database.DbContext;

public class ReadWriteDbContext(DbContextOptions<ReadWriteDbContext> options) : BaseDbContext(options);
