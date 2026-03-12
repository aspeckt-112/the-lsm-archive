using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class PatreonPostEntityConfiguration : BaseEntityConfiguration<PatreonPostEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<PatreonPostEntity> builder)
    {
        builder
            .HasOne(x => x.Show)
            .WithMany(x => x.PatreonPosts)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(x => x.PatreonId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ShowId, x.PatreonId })
            .IsUnique();

        builder
            .Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder
            .Property(x => x.Published)
            .IsRequired();

        builder
            .Property(x => x.Summary)
            .IsRequired()
            .HasMaxLength(10_000);

        builder
            .Property(x => x.Link)
            .IsRequired()
            .HasMaxLength(1000);

        builder
            .Property(x => x.AudioUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder
            .Property(x => x.ProcessingError)
            .IsRequired(false)
            .HasMaxLength(2000);
    }
}
