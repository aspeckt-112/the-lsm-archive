using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class ShowEntityConfiguration : BaseEntityConfiguration<ShowEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<ShowEntity> builder)
    {
        builder
            .Property(x => x.Name)
            .HasMaxLength(150);

        builder
            .Property(x => x.Name)
            .IsRequired();

        builder
            .HasIndex(x => x.Name)
            .IsUnique();

        builder
            .Property(x => x.LastSyncedAt)
            .IsRequired(false);
    }
}
