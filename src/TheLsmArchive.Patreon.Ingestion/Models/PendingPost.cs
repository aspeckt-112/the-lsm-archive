namespace TheLsmArchive.Patreon.Ingestion.Models;

/// <summary>
/// Represents a Patreon post that is pending processing, including its ID, title, and any processing error message if applicable.
/// </summary>
/// <param name="Id">The ID of the pending post, corresponding to the Patreon post ID.</param>
/// <param name="Title">The title of the pending post.</param>
/// <param name="ProcessingError">Any error message encountered during processing, if applicable.</param>
public sealed record PendingPost(int Id, string Title, string? ProcessingError);