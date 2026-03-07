using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class EpisodeEntityConfiguration : BaseEntityConfiguration<EpisodeEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<EpisodeEntity> builder)
    {
        builder
            .HasOne(x => x.Show)
            .WithMany(x => x.Episodes)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.PatreonPost)
            .WithOne(x => x.Episode)
            .HasForeignKey<EpisodeEntity>(x => x.PatreonPostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder
            .Property(x => x.ReleaseDateUtc)
            .IsRequired();
    }
}
