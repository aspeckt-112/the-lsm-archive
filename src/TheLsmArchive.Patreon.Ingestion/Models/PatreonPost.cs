using TheLsmArchive.Database.Entities;

namespace TheLsmArchive.Patreon.Ingestion.Models;

/// <summary>
/// Represents a Patreon post with relevant details.
/// </summary>
/// <param name="Id">The unique identifier of the Patreon post. Note, this is not the database ID, it's the Patreon post ID.</param>
/// <param name="Title">The title of the Patreon post.</param>
/// <param name="Published">The publication date and time of the Patreon post.</param>
/// <param name="Summary">A brief summary or description of the Patreon post.</param>
/// <param name="Link">The URL link to the Patreon post.</param>
/// <param name="AudioUrl">The URL of the audio content associated with the Patreon post.</param>
public record PatreonPost(
    int Id,
    string Title,
    DateTimeOffset Published,
    string Summary,
    string Link,
    string AudioUrl)
{
    /// <summary>
    /// Converts the current <see cref="PatreonPost"/> instance to a <see cref="PatreonPostEntity"/>
    /// </summary>
    /// <param name="showId">The ID of the associated show.</param>
    /// <returns>A new instance of <see cref="PatreonPostEntity"/> populated with data from the current <see cref="PatreonPost"/>.</returns>
    public PatreonPostEntity ToEntity(int showId)
    {
        return new PatreonPostEntity
        {
            Link = Link,
            PatreonId = Id,
            Title = Title,
            AudioUrl = AudioUrl,
            Published = Published.ToUniversalTime(),
            ShowId = showId,
            Summary = Summary,
        };
    }
}
