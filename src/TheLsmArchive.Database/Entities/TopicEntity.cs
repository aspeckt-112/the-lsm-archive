using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

public class TopicEntity : BaseEntity
{
    public string Name { get; set; } = null!;

    public ICollection<TopicEpisodeEntity> TopicEpisodes { get; set; } = [];

    public ICollection<PersonTopicEntity> PersonTopics { get; set; } = [];
}
