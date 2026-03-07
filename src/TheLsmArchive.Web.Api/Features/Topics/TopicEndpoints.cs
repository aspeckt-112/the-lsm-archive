using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;

namespace TheLsmArchive.Web.Api.Features.Topics;

/// <summary>
/// The topic endpoints.
/// </summary>
internal static class TopicEndpoints
{
    /// <summary>
    /// Adds the topic endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        internal WebApplication AddTopicEndpoints()
        {
            RouteGroupBuilder topic = app.MapGroup("/topic").WithTags("Topic");

            topic.MapGet("/{id:int}", GetTopicById)
                .WithName(nameof(GetTopicById))
                .WithSummary("Gets a topic by its ID.")
                .WithDescription("Retrieves the details of a specific topic using its unique identifier.")
                .Produces<Ok<Topic>>()
                .Produces<NotFound>()
                .Produces<BadRequest>();

            topic.MapGet("/{id:int}/details", GetTopicDetailsById)
                .WithName(nameof(GetTopicDetailsById))
                .WithSummary("Gets detailed information about a specific topic.")
                .WithDescription("Retrieves detailed information about a topic, including the dates it was first and last discussed.")
                .Produces<Ok<TopicDetails>>()
                .Produces<NotFound>()
                .Produces<BadRequest>();

            topic.MapGet("/{id:int}/episodes", GetEpisodesByTopicId)
                .WithName(nameof(GetEpisodesByTopicId))
                .WithSummary("Gets topic episodes associated with a specific topic.")
                .WithDescription("Retrieves a list of topic episodes that are associated with the specified topic ID.")
                .Produces<Ok<List<Episode>>>()
                .Produces<BadRequest>();

            topic.MapGet("/{id:int}/people", GetPeopleByTopicId)
                .WithName(nameof(GetPeopleByTopicId))
                .WithSummary("Gets people associated with a specific topic.")
                .WithDescription("Retrieves a list of people that are associated with the specified topic ID.")
                .Produces<Ok<List<Person>>>()
                .Produces<BadRequest>();

            return app;
        }
    }

    private static async Task<Results<Ok<TopicDetails>, NotFound>> GetTopicDetailsById(
        [FromRoute] int id,
        [FromServices] ITopicService topicService,
        CancellationToken cancellationToken)
    {
        TopicDetails? details = await topicService.GetDetailsById(id, cancellationToken);

        return details switch
        {
            null => TypedResults.NotFound(),
            _ => TypedResults.Ok(details)
        };
    }

    private static async Task<Results<Ok<Topic>, NotFound>> GetTopicById(
        [FromRoute] int id,
        [FromServices] ITopicService topicService,
        CancellationToken cancellationToken)
    {
        Topic? topic = await topicService.GetById(id, cancellationToken);

        return topic switch
        {
            null => TypedResults.NotFound(),
            _ => TypedResults.Ok(topic)
        };
    }

    private static async Task<Ok<List<Episode>>> GetEpisodesByTopicId(
        [FromRoute] int id,
        [FromServices] IEpisodeService episodeService,
        CancellationToken cancellationToken)
    {
        List<Episode> episodes = await episodeService.GetByTopicId(id, cancellationToken);
        return TypedResults.Ok(episodes);
    }

    private static async Task<Ok<List<Person>>> GetPeopleByTopicId(
        [FromRoute] int id,
        [FromServices] IPersonService personService,
        CancellationToken cancellationToken)
    {
        List<Person> people = await personService.GetByTopicId(id, cancellationToken);
        return TypedResults.Ok(people);
    }
}
