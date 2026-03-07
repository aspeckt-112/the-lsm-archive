using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class TopicEntityConfiguration : BaseEntityConfiguration<TopicEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<TopicEntity> builder)
    {
        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder
            .HasIndex(x => x.Name)
            .IsUnique();
    }
}
