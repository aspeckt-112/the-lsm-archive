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

            topic.MapGet("/{id:int}/timeline", GetTopicTimeline)
                .WithName(nameof(GetTopicTimeline))
                .WithSummary("Gets the timeline of a topic.")
                .WithDescription("Retrieves the chronological timeline of episodes where the topic was discussed, including people who appeared in each episode.")
                .Produces<Ok<TopicTimeline>>()
                .Produces<NotFound>()
                .Produces<BadRequest>();

            topic.MapGet("/{id:int}/most-discussed-alongside", GetMostDiscussedAlongside)
                .WithName(nameof(GetMostDiscussedAlongside))
                .WithSummary("Gets the topics most frequently discussed alongside a topic.")
                .WithDescription("Retrieves the topics that most frequently co-occur with the specified topic across episodes.")
                .Produces<Ok<List<MostDiscussedTopic>>>()
                .Produces<BadRequest>();

            return app;
        }
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

    private static async Task<Results<Ok<TopicTimeline>, NotFound>> GetTopicTimeline(
        [FromRoute] int id,
        [AsParameters] PagedItemRequest pagedRequest,
        [FromQuery] bool sortDescending = true,
        [FromServices] ITopicService topicService = default!,
        CancellationToken cancellationToken = default)
    {
        TopicTimeline? timeline = await topicService.GetTimeline(id, pagedRequest, sortDescending, cancellationToken);

        return timeline switch
        {
            null => TypedResults.NotFound(),
            _ => TypedResults.Ok(timeline)
        };
    }

    private static async Task<Ok<List<MostDiscussedTopic>>> GetMostDiscussedAlongside(
        [FromRoute] int id,
        [FromServices] ITopicService topicService,
        CancellationToken cancellationToken)
    {
        List<MostDiscussedTopic> topics = await topicService.GetMostDiscussedAlongsideByTopicId(id, cancellationToken);

        return TypedResults.Ok(topics);
    }
}
