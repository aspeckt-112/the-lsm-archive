namespace TheLsmArchive.Domain.Models;

/// <summary>
/// The show model representing a show in the system.
/// </summary>
/// <param name="Id">The unique identifier of the show.</param>
/// <param name="Name">The name of the show.</param>
public sealed record Show(int Id, string Name);
