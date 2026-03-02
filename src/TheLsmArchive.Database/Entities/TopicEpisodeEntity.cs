using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

public class TopicEpisodeEntity : BaseEntity
{
    public int TopicId { get; set; }

    public TopicEntity Topic { get; set; } = null!;

    public int EpisodeId { get; set; }

    public EpisodeEntity Episode { get; set; } = null!;
}
