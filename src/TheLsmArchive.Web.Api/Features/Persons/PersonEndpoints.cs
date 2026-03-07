using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Features.Persons;

/// <summary>
/// The person endpoints.
/// </summary>
internal static class PersonEndpoints
{
    /// <summary>
    /// Adds the person endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        internal WebApplication AddPersonEndpoints()
        {
            RouteGroupBuilder person = app.MapGroup("/person").WithTags("Person");

            person.MapGet("/{id:int}", GetPersonById)
                .WithName(nameof(GetPersonById))
                .WithSummary("Gets a person by its ID.")
                .WithDescription("Retrieves the details of a specific person using its unique identifier.")
                .Produces<Ok<Person>>()
                .Produces<NotFound>()
                .Produces<BadRequest>();

            person.MapGet("/{id:int}/details", GetPersonDetailsById)
                .WithName(nameof(GetPersonDetailsById))
                .WithSummary("Gets detailed information about a specific person.")
                .WithDescription("Retrieves detailed information about a person, including the dates they were first and last mentioned.")
                .Produces<Ok<PersonDetails>>()
                .Produces<NotFound>()
                .Produces<BadRequest>();

            person.MapGet("/{id:int}/topics", GetTopicsByPersonId)
                .WithName(nameof(GetTopicsByPersonId))
                .WithSummary("Gets topics associated with a specific person.")
                .WithDescription("Retrieves a paginated list of topics that are associated with the specified person ID.")
                .Produces<Ok<PagedResponse<Topic>>>()
                .Produces<BadRequest>();

            person.MapGet("/{id:int}/episodes", GetEpisodesByPersonId)
                .WithName(nameof(GetEpisodesByPersonId))
                .WithSummary("Gets episodes associated with a specific person.")
                .WithDescription("Retrieves a paginated list of episodes that are associated with the specified person ID.")
                .Produces<Ok<PagedResponse<Episode>>>()
                .Produces<BadRequest>();

            return app;
        }
    }

    private static async Task<Results<Ok<Person>, NotFound>> GetPersonById(
        [FromRoute] int id,
        [FromServices] IPersonService personService,
        CancellationToken cancellationToken)
    {
        Person? person = await personService.GetById(id, cancellationToken);

        return person switch
        {
            null => TypedResults.NotFound(),
            _ => TypedResults.Ok(person)
        };
    }

    private static async Task<Results<Ok<PersonDetails>, NotFound>> GetPersonDetailsById(
        [FromRoute] int id,
        [FromServices] IPersonService personService,
        CancellationToken cancellationToken)
    {
        PersonDetails? details = await personService.GetDetailsById(id, cancellationToken);

        return details switch
        {
            null => TypedResults.NotFound(),
            _ => TypedResults.Ok(details)
        };
    }

    private static async Task<Ok<PagedResponse<Topic>>> GetTopicsByPersonId(
        [FromRoute] int id,
        [AsParameters] PagedItemRequest pagedRequest,
        [FromServices] ITopicService topicService,
        CancellationToken cancellationToken)
    {
        PagedResponse<Topic> topics = await topicService.GetByPersonId(id, pagedRequest, cancellationToken);
        return TypedResults.Ok(topics);
    }

    private static async Task<Ok<PagedResponse<Episode>>> GetEpisodesByPersonId(
        [FromRoute] int id,
        [AsParameters] PagedItemRequest pagedRequest,
        [FromServices] IEpisodeService episodeService,
        CancellationToken cancellationToken)
    {
        PagedResponse<Episode> episodes = await episodeService.GetByPersonId(id, pagedRequest, cancellationToken);
        return TypedResults.Ok(episodes);
    }
}
