namespace TheLsmArchive.Web.Frontend.Pages;

public partial class EpisodePage
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No episode found with ID {Id}.")]
    private static partial void LogEpisodeNotFound(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No topics found for episode ID {Id}.")]
    private static partial void LogNoTopicsForEpisode(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No persons found for episode ID {Id}.")]
    private static partial void LogNoPersonsForEpisode(ILogger logger, int id);
}
