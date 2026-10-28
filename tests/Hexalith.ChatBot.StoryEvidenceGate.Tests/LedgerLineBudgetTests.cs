using Hexalith.ChatBot.StoryEvidenceGate;

using Shouldly;

namespace Hexalith.ChatBot.StoryEvidenceGate.Tests;

/// <summary>Exercises line limits on local and Git-text ledger parsing paths.</summary>
public sealed class LedgerLineBudgetTests
{
    /// <summary>Admits the exact line ceiling and refuses the first excess line with stable metadata.</summary>
    /// <param name="kind">The ledger entry point.</param>
    /// <param name="newline">The line separator.</param>
    /// <param name="terminated">Whether the last line has a terminator.</param>
    [Theory]
    [InlineData("story", "\n", true)]
    [InlineData("action", "\n", true)]
    [InlineData("ledger", "\n", true)]
    [InlineData("story", "\r\n", true)]
    [InlineData("action", "\r\n", true)]
    [InlineData("ledger", "\r\n", true)]
    [InlineData("story", "\n", false)]
    [InlineData("action", "\n", false)]
    [InlineData("ledger", "\n", false)]
    [InlineData("story", "\r\n", false)]
    [InlineData("action", "\r\n", false)]
    [InlineData("ledger", "\r\n", false)]
    public void LedgerReadersEnforceLineCeilingBeforeParsing(string kind, string newline, bool terminated)
    {
        string[] record = kind switch
        {
            "story" => ["development_status:", "  1-1-fixture: review"],
            "action" => ["  - epic: 1", "    action: \"fixture\"", "    status: open"],
            _ => ["## TE-1 — Fixture", "- **Status:** review"],
        };
        string atLimit = string.Join(newline,
            Enumerable.Repeat(string.Empty, BoundedEvidenceInput.MaximumTextLines - record.Length).Concat(record))
            + (terminated ? newline : string.Empty);
        string excess = atLimit + (terminated ? string.Empty : newline) + "extra";
        string path = Path.Combine(Path.GetTempPath(), "chatbot-ledger-lines-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (bool file in new[] { false, true })
            {
                File.WriteAllText(path, atLimit);
                Read(kind, file, path, atLimit).ShouldBe(kind == "action" ? "open" : "review");
                File.WriteAllText(path, excess);
                GateValidationException error = Should.Throw<GateValidationException>(() => Read(kind, file, path, excess));
                error.ReasonCode.ShouldBe(GateReason.StatusMismatch);
                error.Subject.ShouldBe(kind == "ledger" ? "ledger-input-limit" : "sprint-input-limit");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Fenced examples still cannot introduce ledger statuses or duplicate headings.</summary>
    [Fact]
    public void BoundedTechnicalLedgerPreservesFenceExclusion()
    {
        const string text = "```md\n## TE-1 — Example\n- **Status:** complete\n```\n## TE-1 — Actual\n- **Status:** review\n";
        TechnicalEnablerLedgerReader.StatusFromText(text, "TE-1").ShouldBe("review");
    }

    private static string? Read(string kind, bool file, string path, string text) => (kind, file) switch
    {
        ("story", true) => SprintLedgerReader.StoryStatus(path, "1-1-fixture"),
        ("story", false) => SprintLedgerReader.StoryStatusFromText(text, "1-1-fixture"),
        ("action", true) => SprintLedgerReader.ActionStatus(path, "fixture"),
        ("action", false) => SprintLedgerReader.ActionStatusFromText(text, "fixture"),
        (_, true) => TechnicalEnablerLedgerReader.Status(path, "TE-1"),
        _ => TechnicalEnablerLedgerReader.StatusFromText(text, "TE-1"),
    };
}
