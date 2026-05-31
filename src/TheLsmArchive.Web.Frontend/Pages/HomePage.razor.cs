using TheLsmArchive.Models.Enums;

namespace TheLsmArchive.Web.Frontend.Pages;

public partial class HomePage
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No episodes available for random navigation.")]
    private static partial void LogNoEpisodesAvailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Navigating to {EntityType} with ID {Id}")]
    private static partial void LogNavigatingToEntity(ILogger logger, EntityType entityType, int id);
}
