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
    /// Gets episodes by a person's identifier.
    /// </summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="pagedRequest">The paged request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged response of episodes associated with the person.</returns>
    public Task<PagedResponse<Episode>> GetByPersonId(
        int id,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets episodes by a topic's identifier.
    /// </summary>
    /// <param name="id">The topic's identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of episodes associated with the topic.</returns>
    public Task<List<Episode>> GetByTopicId(
        int id,
        CancellationToken cancellationToken);
}
