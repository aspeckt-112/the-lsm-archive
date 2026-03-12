using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class PersonEpisodeEntityConfiguration : BaseEntityConfiguration<PersonEpisodeEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<PersonEpisodeEntity> builder)
    {
        builder
            .HasOne(x => x.Person)
            .WithMany(x => x.PersonEpisodes)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Episode)
            .WithMany(x => x.PersonEpisodes)
            .HasForeignKey(x => x.EpisodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.PersonId, x.EpisodeId })
            .IsUnique();
    }
}
