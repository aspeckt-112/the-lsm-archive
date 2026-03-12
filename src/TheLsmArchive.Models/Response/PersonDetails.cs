namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents detailed information about a person.
/// </summary>
/// <param name="FirstAppeared">The date when the person first appeared.</param>
/// <param name="LastAppeared">The date when the person last appeared.</param>
public record PersonDetails(
    DateOnly FirstAppeared,
    DateOnly LastAppeared);
