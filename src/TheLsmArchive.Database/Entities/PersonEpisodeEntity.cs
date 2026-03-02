using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

public class PersonEpisodeEntity : BaseEntity
{
    public int PersonId { get; set; }

    public PersonEntity Person { get; set; } = null!;

    public int EpisodeId { get; set; }

    public EpisodeEntity Episode { get; set; } = null!;
}
