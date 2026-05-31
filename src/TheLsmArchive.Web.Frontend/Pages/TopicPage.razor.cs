namespace TheLsmArchive.Web.Frontend.Pages;

public partial class TopicPage
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No topic found with ID {Id}.")]
    private static partial void LogTopicNotFound(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No timeline found for topic ID {Id}.")]
    private static partial void LogNoTimelineForTopic(ILogger logger, int id);
}
