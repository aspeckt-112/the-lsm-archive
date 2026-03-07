using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class PersonEntityConfiguration : BaseEntityConfiguration<PersonEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<PersonEntity> builder)
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
