using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Features.Episodes;

/// <summary>
/// The episode endpoints.
/// </summary>
internal static class EpisodeEndpoints
{
    /// <summary>
    /// Adds the episode endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        internal WebApplication AddEpisodeEndpoints()
        {
            RouteGroupBuilder episode = app.MapGroup("/episode").WithTags("Episode");

            episode.MapGet("/{id:int}", GetEpisodeById)
                .WithName(nameof(GetEpisodeById))
                .WithSummary("Gets an episode by its ID.")
                .WithDescription("Retrieves the details of a specific episode using its unique identifier.")
                .Produces<Ok<Episode>>()
                .Produces<NotFound>()
                .Produces<BadRequest>();

            episode.MapGet("/{id:int}/people", GetPeopleByEpisodeId)
                .WithName(nameof(GetPeopleByEpisodeId))
                .WithSummary("Gets people associated with a specific episode.")
                .WithDescription("Retrieves a list of people who are associated with the specified episode ID.")
                .Produces<Ok<List<Person>>>()
                .Produces<BadRequest>();

            episode.MapGet("/{id:int}/topics", GetTopicsByEpisodeId)
                .WithName(nameof(GetTopicsByEpisodeId))
                .WithSummary("Gets topics associated with a specific episode.")
                .WithDescription("Retrieves a list of topics that are associated with the specified episode ID.")
                .Produces<Ok<List<Topic>>>()
                .Produces<BadRequest>();

            episode.MapGet("/recent", GetRecentEpisodes)
                .WithName(nameof(GetRecentEpisodes))
                .WithSummary("Gets the most recent episodes from the last 7 days.")
                .WithDescription("Retrieves a list of episodes that were released in the last 7 days.")
                .Produces<Ok<List<Episode>>>()
                .Produces<BadRequest>();

            return app;
        }
    }

    private static async Task<Ok<List<Episode>>> GetRecentEpisodes(
        [FromServices] IEpisodeService episodeService,
        CancellationToken cancellationToken)
    {
        List<Episode> episodes = await episodeService.GetRecent(cancellationToken);
        return TypedResults.Ok(episodes);
    }

    private static async Task<Results<Ok<Episode>, NotFound>> GetEpisodeById(
        [FromRoute] int id,
        [FromServices] IEpisodeService episodeService,
        CancellationToken cancellationToken)
    {
        Episode? episode = await episodeService.GetById(id, cancellationToken);

        return episode switch
        {
            null => TypedResults.NotFound(),
            _ => TypedResults.Ok(episode)
        };
    }

    private static async Task<Ok<List<Person>>> GetPeopleByEpisodeId(
        [FromRoute] int id,
        [FromServices] IPersonService personService,
        CancellationToken cancellationToken)
    {
        List<Person> people = await personService.GetByEpisodeId(id, cancellationToken);
        return TypedResults.Ok(people);
    }

    private static async Task<Ok<List<Topic>>> GetTopicsByEpisodeId(
        [FromRoute] int id,
        [FromServices] ITopicService topicService,
        CancellationToken cancellationToken)
    {
        List<Topic> topics = await topicService.GetByEpisodeId(id, cancellationToken);
        return TypedResults.Ok(topics);
    }
}
