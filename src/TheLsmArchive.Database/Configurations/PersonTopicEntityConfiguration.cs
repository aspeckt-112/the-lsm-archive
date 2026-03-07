using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TheLsmArchive.Database.Configurations.Abstractions;
using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Database.Configurations;

public class PersonTopicEntityConfiguration : BaseEntityConfiguration<PersonTopicEntity>
{
    protected override void OnConfigure(EntityTypeBuilder<PersonTopicEntity> builder)
    {
        builder
            .HasOne(x => x.Person)
            .WithMany(x => x.PersonTopics)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Topic)
            .WithMany(x => x.PersonTopics)
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.PersonId, x.TopicId })
            .IsUnique();
    }
}
