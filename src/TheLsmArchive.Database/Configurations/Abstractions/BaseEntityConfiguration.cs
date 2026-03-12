using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Configurations.Abstractions;

public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        OnConfigure(builder);
    }

    protected abstract void OnConfigure(EntityTypeBuilder<T> builder);
}
