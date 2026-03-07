namespace TheLsmArchive.Web.Api.Features.System;

/// <summary>
/// The system endpoints.
/// </summary>
internal static class SystemEndpoints
{
    /// <summary>
    /// Adds the system endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    extension(WebApplication app)
    {
        internal WebApplication AddSystemEndpoints()
        {
            RouteGroupBuilder system = app.MapGroup("/system").WithTags("System");

            system.MapGet("/last-data-sync", GetLastDataSyncDateTime)
                .WithName(nameof(GetLastDataSyncDateTime))
                .WithSummary("Gets the date and time of the last data synchronization.")
                .WithDescription("Retrieves the date and time when the archive was last synchronized with the source data.")
                .Produces<Ok<DateTimeOffset>>();

            return app;
        }
    }

    private static async Task<Ok<DateTimeOffset>> GetLastDataSyncDateTime(
        [FromServices] ISystemService systemService,
        CancellationToken cancellationToken)
    {
        DateTimeOffset lastDataSyncDateTime = await systemService.GetLastDataSyncDateTimeAsync(cancellationToken);
        return TypedResults.Ok(lastDataSyncDateTime);
    }
}
