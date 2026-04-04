namespace TheLsmArchive.Web.Api.Features.Topics;

/// <summary>
/// The abstraction for a service to manage topics.
/// </summary>
public interface ITopicService
{
    /// <summary>
    /// Gets a topic by its identifier.
    /// </summary>
    /// <param name="id">The topic identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The topic if found; otherwise, null.</returns>
    public Task<Topic?> GetById(
        int id,
         CancellationToken cancellationToken);

    /// <summary>
    /// Gets detailed information about a topic by its identifier.
    /// </summary>
    /// <param name="id">The topic identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The topic details if found; otherwise, null.</returns>
    public Task<TopicDetails?> GetDetailsById(
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets episodes by a topic's identifier.
    /// </summary>
    /// <param name="id">The topic identifier.</param>
    /// <param name="pagedRequest">The paged request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged response of episodes associated with the topic.</returns>
    public Task<PagedResponse<Episode>> GetEpisodesByTopicId(
        int id,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets topics by an episode's identifier.
    /// </summary>
    /// <param name="id">The episode identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of topics associated with the episode.</returns>
    public Task<List<Topic>> GetByEpisodeId(
        int id,
         CancellationToken cancellationToken);

    /// <summary>
    /// Gets topics by a person's identifier.
    /// </summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="pagedRequest">The paged request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged response of topics associated with the person.</returns>
    public Task<PagedResponse<Topic>> GetByPersonId(
        int id,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken);
}
