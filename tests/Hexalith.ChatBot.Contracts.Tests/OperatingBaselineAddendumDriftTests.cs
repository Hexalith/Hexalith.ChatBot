using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 8.3 AC6/AC8: the addendum §Operating Baselines table is mirrored from the code catalog (the single source
/// of truth). This drift guard asserts every published metric name appears in the addendum table and the addendum
/// introduces no metric name the code catalog does not publish — catching doc/code drift the way Stories 8.1/7.5
/// kept File-List/claims honest.
/// </summary>
public sealed class OperatingBaselineAddendumDriftTests
{
    private const string AddendumRelativePath =
        "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md";

    [Fact]
    public void AddendumOperatingBaselinesTableShouldMatchTheCodeCatalogMetricNameSet()
    {
        string addendum = File.ReadAllText(ProjectPath(AddendumRelativePath));

        foreach (PublishedSlo slo in OperatingBaselineCatalog.Published)
        {
            // The mirror renders each metric name as an inline-code token in the table.
            addendum.ShouldContain($"`{slo.MetricName}`", customMessage: $"addendum missing published SLO '{slo.MetricName}'");
        }

        // No code-unknown metric name should appear as a published-catalog code token in the doc.
        foreach (string codeToken in ExtractChatbotMetricTokens(addendum))
        {
            OperatingBaselineCatalog.Published
                .Any(slo => slo.MetricName == codeToken)
                .ShouldBeTrue($"addendum publishes metric '{codeToken}' that the code catalog does not");
        }
    }

    [Fact]
    public void AddendumOperatingBaselinesRowsShouldMirrorEveryPublishedFieldNotJustTheMetricName()
    {
        // AC6/AC8: the addendum table mirrors the code catalog with the SAME per-SLO fields. The metric-name guard
        // above catches added/removed SLOs; this guard catches silent VALUE drift (a target/window/budget/threshold/
        // calibration-source/tenant-scope edited in the doc but not the code, or vice-versa). Each SLO's row must
        // carry all seven fields as inline-code tokens on the same table line.
        string[] lines = File.ReadAllText(ProjectPath(AddendumRelativePath)).Split('\n');

        foreach (PublishedSlo slo in OperatingBaselineCatalog.Published)
        {
            string row = lines.SingleOrDefault(line =>
                line.TrimStart().StartsWith("| `" + slo.MetricName + "`", StringComparison.Ordinal))
                ?? throw new ShouldAssertException($"addendum has no published-SLO table row for '{slo.MetricName}'");

            foreach (string field in new[] { slo.Target, slo.MeasurementWindow, slo.ErrorBudget, slo.AlertThreshold, slo.CalibrationSource, slo.TenantScope })
            {
                row.ShouldContain($"`{field}`", customMessage: $"addendum row for '{slo.MetricName}' drifted: missing field token '{field}'");
            }
        }
    }

    private static IEnumerable<string> ExtractChatbotMetricTokens(string text)
    {
        HashSet<string> tokens = new(StringComparer.Ordinal);
        int index = 0;
        while ((index = text.IndexOf("`chatbot.", index, StringComparison.Ordinal)) >= 0)
        {
            int start = index + 1;
            int end = text.IndexOf('`', start);
            if (end < 0)
            {
                break;
            }

            string token = text[start..end];
            // Only consider tokens that look like SLO metric names (dotted, no spaces) — not prose or class refs.
            if (!token.Contains(' ') && token.StartsWith("chatbot.", StringComparison.Ordinal))
            {
                tokens.Add(token);
            }

            index = end + 1;
        }

        return tokens;
    }

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }
}
