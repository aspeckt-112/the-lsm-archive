using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

public class PersonTopicEntity : BaseEntity
{
    public int PersonId { get; set; }

    public PersonEntity Person { get; set; } = null!;

    public int TopicId { get; set; }

    public TopicEntity Topic { get; set; } = null!;
}
