using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

public class PersonEntity : BaseEntity
{
    public string Name { get; set; } = null!;

    public ICollection<PersonEpisodeEntity> PersonEpisodes { get; set; } = [];

    public ICollection<PersonTopicEntity> PersonTopics { get; set; } = [];
}
