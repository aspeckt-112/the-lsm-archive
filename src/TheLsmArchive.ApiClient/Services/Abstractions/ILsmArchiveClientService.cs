using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.ApiClient.Services.Abstractions;

/// <summary>
/// The abstraction for the LSM Archive API client service.
/// </summary>
public interface ILsmArchiveClientService
{
    /// <summary>
    /// Searches the LSM Archive using the specified request.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="PagedResponse{T}"/> of <see cref="SearchResult"/>.</returns>
    public Task<Result<PagedResponse<SearchResult>>> Search(
        SearchRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a person by their ID.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="Person"/>.</returns>
    public Task<Result<Person>> GetPersonById(
        int personId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the most discussed topics associated with a person by their ID.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Result{T}"/> of <see cref="List{T}"/> of <see cref="MostDiscussedTopic"/>.</returns>
    public Task<Result<List<MostDiscussedTopic>>> GetMostDiscussedTopicsByPersonId(
        int personId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets topics associated with a person by their ID.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="pagedRequest">The paged request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Result{T}"/> of <see cref="PagedResponse{T}"/> of <see cref="Topic"/>.</returns>
    public Task<Result<PagedResponse<Topic>>> GetTopicsByPersonId(
        int personId,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets episodes associated with a person by their ID.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="pagedRequest">The paged request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Result{T}"/> of <see cref="PagedResponse{T}"/> of <see cref="Episode"/>.</returns>
    public Task<Result<PagedResponse<Episode>>> GetEpisodesByPersonId(
        int personId,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a topic by its ID.
    /// </summary>
    /// <param name="topicId">The topic ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="Topic"/>.</returns>
    public Task<Result<Topic>> GetTopicById(
        int topicId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the timeline for a topic by its ID.
    /// </summary>
    /// <param name="topicId">The topic ID.</param>
    /// <param name="pagedRequest">The paged request parameters.</param>
    /// <param name="sortDescending">Whether to sort by release date descending (newest first).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="TopicTimeline"/>.</returns>
    public Task<Result<TopicTimeline>> GetTopicTimelineById(
        int topicId,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets detailed information about a person by their ID.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="PersonDetails"/>.</returns>
    public Task<Result<PersonDetails>> GetPersonDetailsById(
        int personId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the most recent episode for a person by their ID.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="Episode"/>.</returns>
    public Task<Result<Episode>> GetLatestEpisodeByPersonId(
        int personId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets an episode by its ID.
    /// </summary>
    /// <param name="episodeId">The episode ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="Episode"/>.</returns>
    public Task<Result<Episode>> GetEpisodeById(
        int episodeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets people associated with an episode by its ID.
    /// </summary>
    /// <param name="episodeId">The episode ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Result{T}"/> of <see cref="List{T}"/> of <see cref="Person"/>.</returns>
    public Task<Result<List<Person>>> GetPersonsByEpisodeId(
        int episodeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets topics associated with an episode by its ID.
    /// </summary>
    /// <param name="episodeId">The episode ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public Task<Result<List<Topic>>> GetTopicsByEpisodeId(
        int episodeId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the most recent episodes from the last 7 days.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> of <see cref="List{T}"/> of <see cref="Episode"/>.</returns>
    public Task<Result<List<Episode>>> GetRecentEpisodes(
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a random episode ID.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> containing a random existing episode ID.</returns>
    public Task<Result<int>> GetRandomEpisodeId(
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the date and time of the last data synchronization.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The date and time of the last data synchronization.</returns>
    /// <remarks>
    /// This method is used to determine when the archive was last synchronized with the source data,
    /// which can be useful for caching and data freshness purposes.
    /// </remarks>
    public Task<Result<DateTimeOffset>> GetLastDataSyncDateTimeAsync(CancellationToken cancellationToken);
}
