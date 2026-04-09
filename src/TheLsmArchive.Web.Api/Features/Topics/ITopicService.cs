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
    /// Gets the timeline for a topic by its identifier.
    /// </summary>
    /// <param name="id">The topic identifier.</param>
    /// <param name="pagedRequest">The paged request parameters.</param>
    /// <param name="sortDescending">Whether to sort by release date descending (newest first).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The topic timeline if found; otherwise, null.</returns>
    public Task<TopicTimeline?> GetTimeline(
        int id,
        PagedItemRequest pagedRequest,
        bool sortDescending,
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
    /// Gets the most discussed topics for a person's identifier.
    /// </summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The top discussed topics associated with the person.</returns>
    public Task<List<MostDiscussedTopic>> GetMostDiscussedByPersonId(
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the topics most frequently discussed alongside a given topic.
    /// </summary>
    /// <param name="id">The topic identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The top co-occurring topics for the given topic.</returns>
    public Task<List<MostDiscussedTopic>> GetMostDiscussedAlongsideByTopicId(
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
