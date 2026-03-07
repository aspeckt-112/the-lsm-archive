namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents an episode.
/// </summary>
/// <param name="Id">The unique identifier of the episode.</param>
/// <param name="Title">The title of the episode.</param>
/// <param name="ReleaseDate">The release date of the episode.</param>
/// <param name="PatreonPostLink">The link to the Patreon post for the episode.</param>
/// <param name="SummaryHtml">The HTML summary of the episode.</param>
public record Episode(
    int Id,
    string Title,
    DateOnly ReleaseDate,
    string PatreonPostLink,
    string SummaryHtml);
