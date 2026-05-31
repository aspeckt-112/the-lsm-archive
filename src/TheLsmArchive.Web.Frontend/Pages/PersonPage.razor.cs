namespace TheLsmArchive.Web.Frontend.Pages;

public partial class PersonPage
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No person found with ID {Id}.")]
    private static partial void LogPersonNotFound(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No person details found for person ID {Id}.")]
    private static partial void LogPersonDetailsNotFound(ILogger logger, int id);

    [LoggerMessage(Level = LogLevel.Information, Message = "No latest episode found for person ID {Id}.")]
    private static partial void LogNoLatestEpisode(ILogger logger, int id);
}
