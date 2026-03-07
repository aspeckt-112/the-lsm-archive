namespace TheLsmArchive.Web.Api.Features.Persons;

/// <summary>
/// The abstraction for a service to manage persons.
/// </summary>
public interface IPersonService
{
    /// <summary>
    /// Gets a person by its identifier.
    /// </summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The person if found; otherwise, null.</returns>
    public Task<Person?> GetById(
        int id,
         CancellationToken cancellationToken);

    /// <summary>
    /// Gets detailed information about a person by its identifier.
    /// </summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The person details if found; otherwise, null.</returns>
    public Task<PersonDetails?> GetDetailsById(
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets persons by an episode's identifier.
    /// </summary>
    /// <param name="id">The episode identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of persons associated with the episode.</returns>
    public Task<List<Person>> GetByEpisodeId(
        int id,
         CancellationToken cancellationToken);

    /// <summary>
    /// Gets persons by a topic's identifier.
    /// </summary>
    /// <param name="id">The topic identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of persons associated with the topic.</returns>
    public Task<List<Person>> GetByTopicId(
        int id,
        CancellationToken cancellationToken);
}
