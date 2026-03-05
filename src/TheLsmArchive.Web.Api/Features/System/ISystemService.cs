namespace TheLsmArchive.Web.Api.Features.System;

/// <summary>
/// The abstraction for a service to provide system-related functionalities.
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Gets the date and time of the last data synchronization.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The date and time of the last data synchronization.</returns>
    public Task<DateTimeOffset> GetLastDataSyncDateTimeAsync(CancellationToken cancellationToken);
}
