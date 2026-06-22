using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Governance guard for Story 13.7 (group sibling titled page-sections in <c>FluentAccordion</c> per the
/// Hexalith UX "Page sections" rule).
/// <para>
/// The Story 13.1 layout-composition guard (<see cref="ChatBotLayoutCompositionConformanceTests"/>) bans
/// hand-rolled page-header / content-box / command-bar / definition-list chrome, but it does NOT enforce
/// accordion grouping. This suite is the positive, build-blocking counterpart for the accordion rule: it pins
/// an explicit accordion-required file list of the surfaces that carry two or more sibling titled
/// <c>&lt;section&gt;</c> regions within a single shell region, and proves each one groups those sections in a
/// single <c>&lt;FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true"&gt;</c> with every item
/// expanded by default — mirroring <c>Hexalith.Tenants.UI</c>
/// <c>DomainUiFluentConformanceTests.Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions</c>.
/// </para>
/// <para>
/// Source-scan based (the ChatBot UI has no bUnit — see memory <c>chatbot-ui-no-bunit-test-strategy</c>); the
/// real-render screenshot gate for these surfaces is Story 13.9. Lives in the build-gated Governance lane so a
/// future edit that drops the accordion grouping (or stops expanding the items by default) fails the build.
/// Single-primary surfaces (OperationalDashboards is deferred to Story 13.5 when it reshapes its data-viz;
/// ComplianceAuditInvestigation is a single primary surface) are intentionally NOT on this list.
/// </para>
/// </summary>
[Trait("Category", "Governance")]
public sealed class Story13AccordionMigrationTests
{
    // The surfaces Story 13.7 owns: each has two or more sibling titled <section> regions inside one shell
    // region (MainContent or ComplementaryPanel). Forward-slash, relative to src/Hexalith.ChatBot.UI.
    // OperationalDashboards.razor (Story 13.5) and ComplianceAuditInvestigation.razor are intentionally excluded.
    private static readonly string[] AccordionRequiredFiles =
    [
        "Components/Governed/ChatBotProjectConversationWorkspace.razor",
        "Components/Pages/AssociationReview.razor",
        "Components/Pages/GovernedOperations.razor",
        "Components/Pages/ProjectWorkspace.razor",
    ];

    [Fact]
    public void Multi_region_surfaces_group_sibling_sections_with_fluent_accordions()
    {
        string uiRoot = UiRoot();
        Directory.Exists(uiRoot).ShouldBeTrue($"ChatBot UI source root not found: {uiRoot}");

        // Non-vacuous: the seeded accordion-required list is real and must be exercised.
        AccordionRequiredFiles.ShouldNotBeEmpty("the accordion-required file list must not be empty.");

        // Missing-path ratchet: a renamed/deleted surface cannot silently keep the guard green.
        List<string> missing = [];
        foreach (string entry in AccordionRequiredFiles)
        {
            string path = Path.Combine(uiRoot, entry.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                missing.Add(entry);
            }
        }

        missing.ShouldBeEmpty(
            "Accordion-required surface paths must exist so a renamed/deleted file cannot silently keep this "
            + "guard green. Missing entries: " + string.Join("; ", missing));

        // Positive: each surface groups its sibling titled sections in a single FluentAccordion, expand-mode
        // Multi, with the items expanded by default (mirrors the Tenants accordion conformance guard).
        List<string> offenders = [];
        foreach (string entry in AccordionRequiredFiles)
        {
            string content = File.ReadAllText(Path.Combine(uiRoot, entry.Replace('/', Path.DirectorySeparatorChar)));
            if (!content.Contains("<FluentAccordion", StringComparison.Ordinal)
                || !content.Contains("ExpandMode=\"AccordionExpandMode.Multi\"", StringComparison.Ordinal)
                || !content.Contains("Expanded=\"true\"", StringComparison.Ordinal))
            {
                offenders.Add(entry);
            }
        }

        offenders.ShouldBeEmpty(
            "Multi-region ChatBot surfaces must group their sibling titled sections in a single "
            + "<FluentAccordion ExpandMode=\"AccordionExpandMode.Multi\" Block=\"true\"> with every "
            + "FluentAccordionItem Expanded=\"true\" (UX \"Page sections\" rule, Story 13.7). Missing accordion "
            + "grouping in: " + string.Join("; ", offenders));
    }

    private static string UiRoot()
        => Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.UI");

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
