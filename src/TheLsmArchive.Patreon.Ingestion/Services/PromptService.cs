using System.Text;

using TheLsmArchive.Common.Constants;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The system instruction service.
/// </summary>
public class PromptService
{
    private static readonly Dictionary<string, string> _knownPersonAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "Matty", "MrMattyPlays" },
            { "Mr Matty Plays", "MrMattyPlays" },
            { "Matthew Gerrity", "MrMattyPlays" } // Matty is sometimes flagged as Matthew Gerrity for some reason. Filter that out.
        };

    /// <summary>
    /// Gets the summary system prompt for the given show name.
    /// </summary>
    /// <param name="showName">The show name.</param>
    /// <param name="knownPersons">Optional list of known persons (hosts and frequent guests) for context.</param>
    /// <param name="knownTopics">Optional list of known topics for context.</param>
    public string GetSummarySystemPrompt(
        string showName,
        IEnumerable<string>? knownPersons = null,
        IEnumerable<string>? knownTopics = null)
    {
        var sb = new StringBuilder();

        // 1. Persona & Goal
        sb.AppendLine("Extract metadata from podcast titles and descriptions.");
        sb.AppendLine("Ignore any timestamps or timecodes (e.g., '0:00:00') in the description.");

        // 2. Output Rules
        sb.AppendLine("Output strictly valid JSON.");
        sb.AppendLine("Return exactly one JSON object with this shape: {\"hosts\": [], \"guests\": [], \"topics\": []}.");
        sb.AppendLine("Do not include markdown, code fences, prose, comments, or additional properties.");
        sb.AppendLine("All array values must contain only strings.");
        sb.AppendLine("Extract the names of the hosts into the 'hosts' array.");
        sb.AppendLine("Extract the names of any guests into the 'guests' array. Guests are typically people who are NOT regular hosts of the show but are invited for a specific episode.");
        sb.AppendLine("Use full, properly capitalized names for people whenever possible.");
        sb.AppendLine("Do not include duplicate names in arrays.");
        sb.AppendLine("A person must not appear in both 'hosts' and 'guests'.");

        if (knownPersons != null && knownPersons.Any())
        {
            sb.AppendLine("To ensure data quality, refer to the following list of known people frequently associated with this show (hosts and frequent guests). If a person in the description is an exact match, a clear alias, or an unambiguous near-match, use the canonical name from this list:");
            foreach (string person in knownPersons.OrderBy(p => p))
            {
                sb.AppendLine($"- {person}");
            }
        }

        sb.AppendLine("Alias handling: If a person is referenced by a nickname, abbreviation, handle, or alternate real name, map them to the canonical name below and output only the canonical name.");
        foreach ((string alias, string canonicalName) in _knownPersonAliases.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"- {alias} => {canonicalName}");
        }

        sb.AppendLine("Extract specific subjects and broader thematic categories that are clearly present in the title or description.");
        sb.AppendLine("Ensure that the topics are capitalized and concise (e.g., 'Game Design', not 'In this episode we talk about game design and development').");
        sb.AppendLine("Prefer common names for topics (e.g., 'Game Pass' instead of 'Xbox Game Pass', 'PlayStation 5' instead of 'PS5') unless the known topic list below provides a canonical alternative.");

        if (knownTopics != null && knownTopics.Any())
        {
            sb.AppendLine("To ensure data consistency, refer to the following list of topics recently discussed on this show. If a topic in the description is a direct match or a synonymous variation of one on this list (e.g. 'PlayStation 5' vs 'PS5'), use the name from this list.");
            sb.AppendLine("However, ensure that distinct entries like sequels, specific versions, or adaptations (e.g., 'Fallout' vs 'Fallout 3', or 'The Last of Us' vs 'The Last of Us HBO') remain separate and specific.");
            foreach (string topic in knownTopics.OrderBy(t => t))
            {
                sb.AppendLine($"- {topic}");
            }
        }

        sb.AppendLine("Be comprehensive but avoid overgeneralization.");

        sb.AppendLine("If specific data is missing, return empty arrays.");
        sb.AppendLine("If episode numbering is missing or ambiguous, prioritize explicit names in the title or description. Otherwise apply the show context defaults below.");

        // 3. Show Specific Context
        switch (showName)
        {
            case ShowName.KnockBack:
                sb.AppendLine(
                    "Context: Unless stated otherwise in the title or description, the hosts are strictly 'Colin Moriarty' and 'Dagan Moriarty'.");
                break;
            case ShowName.QAndAsFiresideChatsAndPatreonExclusives:
                sb.AppendLine(
                    "Context: The host is always 'Colin Moriarty'. There may be guests - they might be named in the title or description. The topics vary widely and may also be named in the title or description.");
                break;
            case ShowName.SacredSymbols:
                sb.AppendLine(
                    "Context: Before episode 150, the hosts are 'Colin Moriarty' and 'Chris Ray Gun'. After and including episode 150, the hosts are 'Colin Moriarty', 'Chris Ray Gun' and 'Dustin Furman'.");
                break;
            case ShowName.SummonSign:
                sb.AppendLine(
                    "Context: 'Brad Ellis' is always the host. Guests may be named in the title or description. The topics vary widely and may also be named in the title or description.");
                break;
            case ShowName.SacredSymbolsPlus:
                sb.AppendLine(
                    "Context: 'Colin Moriarty' is usually the host, but there may be guests - they might be named in the title or description. The topics vary widely and may also be named in the title or description.");
                break;
            case ShowName.DefiningDuke:
                sb.AppendLine(
                    "Context: Up to episode 25, the hosts were 'MrMattyPlays' and 'Jeremy Penter'. From episode 26, the hosts are 'MrMattyPlays' and 'Lord Cognito'. Sometimes there are guests - they might be named in the title or description.");
                break;
            case ShowName.PunchingUp:
                sb.AppendLine(
                    "Context: From episode 1 to episode 24, the hosts were 'Dustin Furman', 'Dagan Moriarty', 'Micah Moriarty' and 'Gene Park'. From episode 25 to episode 38, the hosts were 'Dustin Furman', 'Micah Moriarty' and 'Gene Park'. From episode 39, the hosts are 'Brad Ellis', 'Micah Moriarty' and 'Gene Park'. Sometimes there are guests - they might be named in the title or description.");
                break;
            default:
                break;
        }

        return sb.ToString();
    }
}
