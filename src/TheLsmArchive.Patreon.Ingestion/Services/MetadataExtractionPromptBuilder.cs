using System.Text;

using TheLsmArchive.Common.Constants;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// Builds system prompts for metadata extraction.
/// </summary>
public sealed class MetadataExtractionPromptBuilder
{
    private const string BaseInstructions = """
                                            # Role
                                            You are a metadata extraction assistant specializing in podcast content.

                                            # Task
                                            Extract specific metadata from the provided podcast title and description.
                                            """;

    private static readonly Dictionary<string, string> _knownPersonAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "Matty", "MrMattyPlays" },
            { "Mr Matty Plays", "MrMattyPlays" },
            {
                "Matthew Gerrity", "MrMattyPlays"
            } // Matty is sometimes flagged as Matthew Gerrity for some reason. Filter that out.
        };

    /// <summary>
    /// Builds the system prompt for the given show name.
    /// </summary>
    /// <param name="showName">The show name.</param>
    /// <param name="knownPersons">Optional list of known persons (hosts and frequent guests) for context.</param>
    /// <param name="knownTopics">Optional list of known topics for context.</param>
    public string BuildSystemPrompt(
        string showName,
        IList<string>? knownPersons = null,
        IList<string>? knownTopics = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine(BaseInstructions);
        sb.AppendLine();
        sb.AppendLine("# General Extraction Rules");
        sb.AppendLine("- Ignore timestamps or timecodes (e.g., '0:00:00').");
        sb.AppendLine("- Use full, properly capitalized names for people.");
        sb.AppendLine("- Ensure names are unique; a person cannot be both a host and a guest.");
        sb.AppendLine("- Topics should be concise and capitalized (e.g., 'Game Design').");
        sb.AppendLine("- Prefer common names for topics (e.g., 'Game Pass' instead of 'Xbox Game Pass').");
        sb.AppendLine("- If no data is found for a field, return an empty array [].");

        if (knownPersons is { Count: > 0 })
        {
            sb.AppendLine("\n# Known People (Canonical Names)");
            sb.AppendLine("If a person mentioned matches or is an alias for one of these, use this exact string:");
            foreach (string person in knownPersons.OrderBy(p => p)) sb.AppendLine($"- {person}");
        }

        sb.AppendLine("\n# Alias Mapping");
        sb.AppendLine("Map these specific variations to their canonical forms:");

        foreach ((string alias, string canonicalName) in _knownPersonAliases.OrderBy(kvp => kvp.Key))
            sb.AppendLine($"- {alias} => {canonicalName}");

        if (knownTopics is { Count: > 0 })
        {
            sb.AppendLine("\n# Known Topics");
            sb.AppendLine("Use these names for consistency if the topic matches:");
            foreach (string topic in knownTopics.OrderBy(t => t)) sb.AppendLine($"- {topic}");
        }

        AppendShowContext(sb, showName);

        return sb.ToString();
    }

    private static void AppendShowContext(StringBuilder sb, string showName)
    {
        sb.AppendLine("\n# Show-Specific Context");
        sb.AppendLine("Use the following rules to determine hosts if they are not explicitly mentioned:");

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
                    "Context: Up to episode 25, the hosts were 'MrMattyPlays' and 'Jeremy Penter'. From episode 26, the hosts are 'MrMattyPlays' and 'Lord Cognito'. Sometimes there are guests.");
                break;
            case ShowName.PunchingUp:
                sb.AppendLine(
                    "Context: From episode 1 to episode 24, the hosts were 'Dustin Furman', 'Dagan Moriarty', 'Micah Moriarty' and 'Gene Park'. From episode 25 to episode 38, the hosts were 'Dustin Furman', 'Micah Moriarty' and 'Gene Park'. From episode 39, the hosts are 'Brad Ellis', 'Micah Moriarty' and 'Gene Park'. Sometimes there are guests.");
                break;
        }
    }
}


