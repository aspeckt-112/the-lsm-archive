using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

public class EpisodeEntity : BaseEntity
{
    public int ShowId { get; set; }

    public ShowEntity Show { get; set; } = null!;

    public string Title { get; set; } = null!;

    public DateTimeOffset ReleaseDateUtc { get; set; }

    public ICollection<PersonEpisodeEntity> PersonEpisodes { get; set; } = [];

    public ICollection<TopicEpisodeEntity> TopicEpisodes { get; set; } = [];

    public int PatreonPostId { get; set; }

    public PatreonPostEntity PatreonPost { get; set; } = null!;
}
