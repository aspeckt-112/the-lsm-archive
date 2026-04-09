namespace TheLsmArchive.Web.Api.Features.Episodes;

/// <summary>
/// The abstraction for a service to manage episodes.
/// </summary>
public interface IEpisodeService
{
    /// <summary>
    /// Gets an episode by its identifier.
    /// </summary>
    /// <param name="id">The episode identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>>The episode if found; otherwise, null.</returns>
    public Task<Episode?> GetById(
        int id,
         CancellationToken cancellationToken);

    /// <summary>
    /// Gets timeline entries for episodes by a person's identifier.
    /// </summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="pagedRequest">The paged request.</param>
    /// <param name="sortDescending">Whether to sort by release date descending (newest first).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged response of person timeline entries associated with the person.</returns>
    public Task<PagedResponse<PersonTimelineEntry>> GetByPersonId(
        int id,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the most recent episodes from the last 7 days.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of recent episodes.</returns>
    public Task<List<Episode>> GetRecent(
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the most recently released episode for a person.
    /// </summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The most recent episode for the person if found; otherwise, null.</returns>
    public Task<Episode?> GetMostRecentByPersonId(
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a random episode ID.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A random existing episode ID.</returns>
    public Task<int> GetRandomEpisodeId(
        CancellationToken cancellationToken);
}
