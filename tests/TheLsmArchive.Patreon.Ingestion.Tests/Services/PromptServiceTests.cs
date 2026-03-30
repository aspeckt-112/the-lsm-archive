using TheLsmArchive.Common.Constants;
using TheLsmArchive.Patreon.Ingestion.Services;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class PromptServiceTests
{
    private readonly PromptService _promptService = new();

    [Theory]
    [InlineData(ShowName.KnockBack)]
    [InlineData(ShowName.SacredSymbols)]
    [InlineData(ShowName.SacredSymbolsPlus)]
    [InlineData(ShowName.SummonSign)]
    [InlineData(ShowName.DefiningDuke)]
    [InlineData(ShowName.PunchingUp)]
    [InlineData(ShowName.QAndAsFiresideChatsAndPatreonExclusives)]
    public void GetSummarySystemPrompt_WithKnownShow_ContainsShowContext(string showName)
    {
        string prompt = _promptService.GetSummarySystemPrompt(showName);

        Assert.Contains("Context:", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithUnknownShow_DoesNotContainContext()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Unknown Show");

        Assert.DoesNotContain("Context:", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_AlwaysContainsOutputRules()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Any Show");

        Assert.Contains("Output strictly valid JSON", prompt);
        Assert.Contains("hosts", prompt);
        Assert.Contains("guests", prompt);
        Assert.Contains("topics", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithKnownPersons_IncludesPersonList()
    {
        string[] persons = ["Colin Moriarty", "Dustin Furman"];

        string prompt = _promptService.GetSummarySystemPrompt("Any Show", knownPersons: persons);

        Assert.Contains("Colin Moriarty", prompt);
        Assert.Contains("Dustin Furman", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithKnownTopics_IncludesTopicList()
    {
        string[] topics = ["PlayStation 5", "Game Design"];

        string prompt = _promptService.GetSummarySystemPrompt("Any Show", knownTopics: topics);

        Assert.Contains("PlayStation 5", prompt);
        Assert.Contains("Game Design", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithNullPersons_DoesNotContainKnownPeopleSection()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Any Show", knownPersons: null);

        Assert.DoesNotContain("known people frequently associated", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithEmptyPersons_DoesNotContainKnownPeopleSection()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Any Show", knownPersons: []);

        Assert.DoesNotContain("known people frequently associated", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithNullTopics_DoesNotContainKnownTopicsSection()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Any Show", knownTopics: null);

        Assert.DoesNotContain("topics recently discussed", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_WithEmptyTopics_DoesNotContainKnownTopicsSection()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Any Show", knownTopics: []);

        Assert.DoesNotContain("topics recently discussed", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_AlwaysContainsAliasMap()
    {
        string prompt = _promptService.GetSummarySystemPrompt("Any Show");

        Assert.Contains("Matty => MrMattyPlays", prompt);
        Assert.Contains("Mr Matty Plays => MrMattyPlays", prompt);
        Assert.Contains("Matthew Gerrity => MrMattyPlays", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_KnockBack_ContainsColinAndDagan()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.KnockBack);

        Assert.Contains("Colin Moriarty", prompt);
        Assert.Contains("Dagan Moriarty", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_SacredSymbols_ContainsHostInfo()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.SacredSymbols);

        Assert.Contains("Colin Moriarty", prompt);
        Assert.Contains("Chris Ray Gun", prompt);
        Assert.Contains("Dustin Furman", prompt);
        Assert.Contains("episode 150", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_SacredSymbolsPlus_ContainsColin()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.SacredSymbolsPlus);

        Assert.Contains("Colin Moriarty", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_SummonSign_ContainsBradEllis()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.SummonSign);

        Assert.Contains("Brad Ellis", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_DefiningDuke_ContainsHostTransition()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.DefiningDuke);

        Assert.Contains("MrMattyPlays", prompt);
        Assert.Contains("Jeremy Penter", prompt);
        Assert.Contains("Lord Cognito", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_PunchingUp_ContainsHostTransitions()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.PunchingUp);

        Assert.Contains("Dustin Furman", prompt);
        Assert.Contains("Dagan Moriarty", prompt);
        Assert.Contains("Micah Moriarty", prompt);
        Assert.Contains("Gene Park", prompt);
        Assert.Contains("Brad Ellis", prompt);
    }

    [Fact]
    public void GetSummarySystemPrompt_QAndAs_ContainsColin()
    {
        string prompt = _promptService.GetSummarySystemPrompt(ShowName.QAndAsFiresideChatsAndPatreonExclusives);

        Assert.Contains("Colin Moriarty", prompt);
    }
}
