using TheLsmArchive.Models.Enums;

namespace TheLsmArchive.Models.Response;

public record SearchResult(int Id, string Matched, EntityType EntityType);
