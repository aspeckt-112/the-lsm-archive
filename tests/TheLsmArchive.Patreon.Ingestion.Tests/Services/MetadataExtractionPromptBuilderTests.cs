using TheLsmArchive.Common.Constants;
using TheLsmArchive.Patreon.Ingestion.Services;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public class MetadataExtractionPromptBuilderTests
{
    private readonly MetadataExtractionPromptBuilder _sut = new();

    // -------------------------------------------------------------------------
    // Base instructions
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesRoleHeader()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("# Role", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesTaskHeader()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("# Task", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesGeneralExtractionRules()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("# General Extraction Rules", prompt);
        Contains("Ignore timestamps", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesAliasMappingHeader()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("# Alias Mapping", prompt);
    }

    // -------------------------------------------------------------------------
    // Alias mapping entries
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesMattyAlias()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("- Matty => MrMattyPlays", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesMrMattyPlaysAlias()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("- Mr Matty Plays => MrMattyPlays", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesMatthewGerrityAlias()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("- Matthew Gerrity => MrMattyPlays", prompt);
    }

    // -------------------------------------------------------------------------
    // Known persons section
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildSystemPrompt_IncludesKnownPersonsSection_WhenProvided()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownPersons: ["Colin Moriarty", "Brad Ellis"]);

        Contains("# Known People (Canonical Names)", prompt);
        Contains("- Brad Ellis", prompt);
        Contains("- Colin Moriarty", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SortsKnownPersonsAlphabetically()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownPersons: ["Zara Doe", "Aaron Smith", "Colin Moriarty"]);

        int posAaron = prompt.IndexOf("Aaron Smith", StringComparison.Ordinal);
        int posColin = prompt.IndexOf("Colin Moriarty", StringComparison.Ordinal);
        int posZara = prompt.IndexOf("Zara Doe", StringComparison.Ordinal);

        True(posAaron < posColin && posColin < posZara);
    }

    [Fact]
    public void BuildSystemPrompt_OmitsKnownPersonsSection_WhenNull()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownPersons: null);

        DoesNotContain("# Known People", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_OmitsKnownPersonsSection_WhenEmpty()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownPersons: []);

        DoesNotContain("# Known People", prompt);
    }

    // -------------------------------------------------------------------------
    // Known topics section
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildSystemPrompt_IncludesKnownTopicsSection_WhenProvided()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownTopics: ["Game Design", "Xbox"]);

        Contains("# Known Topics", prompt);
        Contains("- Game Design", prompt);
        Contains("- Xbox", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SortsKnownTopicsAlphabetically()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownTopics: ["Zelda", "Elden Ring", "Astro Bot"]);

        int sectionStart = prompt.IndexOf("# Known Topics", StringComparison.Ordinal);
        int posAstroBot = prompt.IndexOf("Astro Bot", sectionStart, StringComparison.Ordinal);
        int posEldenRing = prompt.IndexOf("Elden Ring", sectionStart, StringComparison.Ordinal);
        int posZelda = prompt.IndexOf("Zelda", sectionStart, StringComparison.Ordinal);

        True(posAstroBot < posEldenRing && posEldenRing < posZelda);
    }

    [Fact]
    public void BuildSystemPrompt_OmitsKnownTopicsSection_WhenNull()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownTopics: null);

        DoesNotContain("# Known Topics", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_OmitsKnownTopicsSection_WhenEmpty()
    {
        string prompt = _sut.BuildSystemPrompt("any show", knownTopics: []);

        DoesNotContain("# Known Topics", prompt);
    }

    // -------------------------------------------------------------------------
    // Show-specific context
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildSystemPrompt_AlwaysIncludesShowSpecificContextHeader()
    {
        string prompt = _sut.BuildSystemPrompt("any show");

        Contains("# Show-Specific Context", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_KnockBack_IncludesColinAndDaganMoriartyAsHosts()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.KnockBack);

        Contains("Colin Moriarty", prompt);
        Contains("Dagan Moriarty", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_QAndAsFiresideChats_IncludesColinMoriartyAsAlwaysHost()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.QAndAsFiresideChatsAndPatreonExclusives);

        Contains("Colin Moriarty", prompt);
        Contains("always", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SacredSymbols_IncludesEpisode150ThresholdAndDustinFurman()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.SacredSymbols);

        Contains("episode 150", prompt);
        Contains("Dustin Furman", prompt);
        Contains("Chris Ray Gun", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SummonSign_IncludesBradEllisAsAlwaysHost()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.SummonSign);

        Contains("Brad Ellis", prompt);
        Contains("always the host", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SacredSymbolsPlus_IncludesColinMoriartyAsUsuallyHost()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.SacredSymbolsPlus);

        Contains("Colin Moriarty", prompt);
        Contains("usually the host", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_DefiningDuke_IncludesEpisode26ThresholdAndLordCognito()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.DefiningDuke);

        Contains("MrMattyPlays", prompt);
        Contains("episode 26", prompt);
        Contains("Lord Cognito", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_PunchingUp_IncludesEpisode39ThresholdAndBradEllis()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.PunchingUp);

        Contains("episode 39", prompt);
        Contains("Brad Ellis", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_UnknownShow_IncludesContextHeaderButNoContextLine()
    {
        string prompt = _sut.BuildSystemPrompt(ShowName.Constellation);

        Contains("# Show-Specific Context", prompt);
        DoesNotContain("Context:", prompt);
    }
}
