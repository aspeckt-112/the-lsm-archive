using System.Text;

using TheLsmArchive.Common.Constants;

namespace TheLsmArchive.Patreon.Ingestion.Services;

/// <summary>
/// The system instruction service.
/// </summary>
public class PromptService
{
    /// <summary>
    /// Gets the summary system prompt for the given show name.
    /// </summary>
    public string GetSummarySystemPrompt(string showName)
    {
        var sb = new StringBuilder();

        // 1. Persona & Goal
        sb.AppendLine("You are an expert podcast archivist.");
        sb.AppendLine("Your task is to extract metadata from podcast titles and descriptions.");
        sb.AppendLine("Ignore any timestamps or timecodes (e.g., '0:00:00') in the description.");

        // 2. Output Rules
        sb.AppendLine("Output strictly valid JSON.");
        sb.AppendLine("Extract the names of the hosts into the 'hosts' array.");
        sb.AppendLine("Ensure that the host first names and surnames are capitalized and separated (e.g., 'Colin Moriarty' not 'colin' or 'ColinDagan').");
        sb.AppendLine("Use full names for known hosts where possible (e.g., 'Colin Moriarty' instead of 'Colin', 'Dagan Moriarty' instead of 'Dagan').");
        sb.AppendLine("Extract the main topics discussed into the 'topics' array.");
        sb.AppendLine("Ensure that the topics are capitalized and concise (e.g., 'Game Design', not 'In this episode we talk about game design and development').");
        sb.AppendLine("Prefer common names for topics (e.g., 'Game Pass' instead of 'Xbox Game Pass', 'PlayStation 5' instead of 'PS5').");
        sb.AppendLine("If specific data is missing, return empty arrays.");

        // 3. Show Specific Context
        switch (showName)
        {
            case ShowName.KnockBack:
                sb.AppendLine(
                    "Context: Unless stated otherwise in the title or description, the hosts are strictly 'Colin Moriarty' and 'Dagan Moriarty'.");
                break;
            case ShowName.QAndAsFiresideChatsAndPatreonExclusives:
                sb.AppendLine(
                    "Context: The host is always 'Colin Moriarty'. There may be guests - they might be named in the title or description. The topic vary widely and may also be named in the title or description.");
                break;
            case ShowName.SacredSymbols:
                sb.AppendLine(
                    "Context: Before episode 150, the hosts are 'Colin Moriarty' and 'Chris Ray Gun'. After and including episode 150, the hosts are 'Colin Moriarty', 'Chris Ray Gun' and 'Dustin Furman'.");
                break;
            case ShowName.SummonSign:
                sb.AppendLine(
                    "Context: 'Brad Ellis' is always the host. Guests may be named in the title or description. The topic vary widely and may also be named in the title or description.");
                break;
            case ShowName.SacredSymbolsPlus:
                sb.AppendLine(
                    "Context: 'Colin Moriarty' is usually the host, but there may be guests - they might be named in the title or description. The topic vary widely and may also be named in the title or description.");
                break;
            case ShowName.DefiningDuke:
                sb.AppendLine(
                    "Context: Up to episode 25, the hosts were 'MrMattyPlays' and 'Jeremy Penter'. From episode 26, the hosts  are 'MrMattyPlays' and 'Lord Cognito'. Sometimes there are guests - they might be named in the title or description");
                break;
            case ShowName.PunchingUp:
                sb.AppendLine(
                    "Context: From episode 1 to episode 24, the hosts were 'Dustin Furman', 'Dagan Moriarty', 'Micah Moriarty' and 'Gene Park'. From episode 25 to episode 38, the hosts were 'Dustin Furman', 'Micah Moriarty' and 'Gene Park'. From episode 39, the hosts are 'Brad Ellis', 'Micah Moriarty' and 'Gene Park'. Sometimes there are guests - they might be named in the title or description");
                break;
            default:
                break;
        }

        return sb.ToString();
    }
}
