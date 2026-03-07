using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class TopicEpisodeEntityConfiguration : BaseEntityConfiguration<TopicEpisodeEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<TopicEpisodeEntity> builder)
    {
        builder
            .HasOne(x => x.Topic)
            .WithMany(x => x.TopicEpisodes)
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Episode)
            .WithMany(x => x.TopicEpisodes)
            .HasForeignKey(x => x.EpisodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.TopicId, x.EpisodeId })
            .IsUnique();
    }
}
