namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents a person.
/// </summary>
/// <param name="Id">The person ID.</param>
/// <param name="Name">The person name.</param>
public record Person(
    int Id,
    string Name);
