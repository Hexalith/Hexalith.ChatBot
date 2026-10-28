using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;

using Hexalith.ChatBot.StoryEvidenceGate;

using Shouldly;

namespace Hexalith.ChatBot.StoryEvidenceGate.Tests;

/// <summary>Exercises structural XML budgets before evidence document-tree construction.</summary>
public static class TrxStructuralBudgetTests
{
    /// <summary>Proves all TRX consumers refuse structural amplification while preserving existing evidence.</summary>
    [Theory]
    [InlineData("depth")]
    [InlineData("nodes")]
    [InlineData("attributes")]
    public static void StructuralAmplificationShouldFailClosedInEveryConsumer(string kind)
    {
        using GateFixture fixture = new();
        string extension = Amplification(kind);
        byte[] sidecarBefore = File.ReadAllBytes(fixture.ProvenancePath);
        string ordinary = File.ReadAllText(fixture.TrxPath)
            .Replace("</TestRun>", extension + "</TestRun>", StringComparison.Ordinal);
        File.WriteAllText(fixture.TrxPath, ordinary);

        GateValidationException attestation = Should.Throw<GateValidationException>(() => fixture.Attest());
        attestation.ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        attestation.Subject.ShouldBe("gate-unit");
        File.ReadAllBytes(fixture.ProvenancePath).ShouldBe(sidecarBefore);

        fixture.MutateProvenance(sidecar => sidecar["trxSha256"] =
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixture.TrxPath))).ToLowerInvariant());
        fixture.Validate().Issues.Single().ShouldBe(new GateIssue(GateReason.MachineResultsInvalid, "gate-unit"));
        File.Delete(fixture.ProvenancePath);
        Should.Throw<GateValidationException>(() => fixture.Attest());
        File.Exists(fixture.ProvenancePath).ShouldBeFalse();

        string raw = Path.Combine(fixture.ResultsRoot, "raw.trx");
        string sanitized = Path.Combine(fixture.ResultsRoot, "sanitized.trx");
        string summaryPath = Path.Combine(fixture.ResultsRoot, "attempt.json");
        File.WriteAllText(raw, RecoveryTrx().Replace("</TestRun>", extension + "</TestRun>", StringComparison.Ordinal));
        File.WriteAllText(sanitized, "existing evidence");
        GateValidationException projection = Should.Throw<GateValidationException>(() =>
            RecoveryTrxSanitizer.Sanitize(raw, sanitized));
        projection.ReasonCode.ShouldBe(GateReason.MachineResultsInvalid);
        projection.Subject.ShouldBe("recovery-sanitize");
        File.ReadAllText(sanitized).ShouldBe("existing evidence");
        File.Delete(sanitized);
        Should.Throw<GateValidationException>(() => RecoveryTrxSanitizer.Sanitize(raw, sanitized));
        File.Exists(sanitized).ShouldBeFalse();

        JsonObject policy = EvidenceJson.LoadPolicy(fixture.PolicyPath);
        RecoveryAttemptSummarizer.Summarize(raw, "failure", summaryPath, policy);
        JsonObject summary = JsonNode.Parse(File.ReadAllText(summaryPath))!.AsObject();
        summary["trxPresent"]!.GetValue<bool>().ShouldBeTrue();
        summary["trxState"]!.GetValue<string>().ShouldBe("malformed");
        summary.ContainsKey("counters").ShouldBeFalse();
        EvidenceJson.ValidateMetadataOnly(summary, policy);
    }

    /// <summary>Proves depth zero is the root and the configured greatest depth is inclusive.</summary>
    [Fact]
    public static void XmlDepthShouldAcceptItsExactBoundaryAndRejectTheNextLevel()
    {
        string xml = "<r>" + Repeat("<n>", BoundedTrxDocument.MaximumDepth)
            + Repeat("</n>", BoundedTrxDocument.MaximumDepth) + "</r>";

        BoundedTrxDocument.Load(Encoding.UTF8.GetBytes(xml)).Root!.Descendants().Count()
            .ShouldBe(BoundedTrxDocument.MaximumDepth);
        string excess = xml.Replace("<r>", "<r><n>", StringComparison.Ordinal)
            .Replace("</r>", "</n></r>", StringComparison.Ordinal);
        Should.Throw<XmlException>(() => BoundedTrxDocument.Load(Encoding.UTF8.GetBytes(excess)))
            .Message.ShouldContain("structural budget");
    }

    /// <summary>Proves reader nodes and attributes share one inclusive aggregate structural budget.</summary>
    [Fact]
    public static void XmlNodeUnitsShouldCountAttributesAtTheExactBoundary()
    {
        string children = Repeat("<n/>", BoundedTrxDocument.MaximumNodeUnits - 3);
        string xml = "<r a=\"\">" + children + "</r>";

        BoundedTrxDocument.Load(Encoding.UTF8.GetBytes(xml)).Root!.Elements().Count()
            .ShouldBe(BoundedTrxDocument.MaximumNodeUnits - 3);
        string excess = xml.Replace("<r a=\"\">", "<r a=\"\" b=\"\">", StringComparison.Ordinal);
        Should.Throw<XmlException>(() => BoundedTrxDocument.Load(Encoding.UTF8.GetBytes(excess)))
            .Message.ShouldContain("structural budget");
    }

    /// <summary>Proves comments and end-element nodes consume the same budget as element starts.</summary>
    [Fact]
    public static void XmlNodeUnitsShouldIncludeCommentsAndClosingElements()
    {
        string xml = "<r>" + Repeat("<!--x-->", BoundedTrxDocument.MaximumNodeUnits - 2) + "</r>";

        BoundedTrxDocument.Load(Encoding.UTF8.GetBytes(xml)).Root!.Nodes().Count()
            .ShouldBe(BoundedTrxDocument.MaximumNodeUnits - 2);
        Should.Throw<XmlException>(() => BoundedTrxDocument.Load(
            Encoding.UTF8.GetBytes(xml.Replace("</r>", "<!--x--></r>", StringComparison.Ordinal))))
            .Message.ShouldContain("structural budget");
    }

    private static string Amplification(string kind) => kind switch
    {
        "depth" => "<Extra>" + Repeat("<n>", BoundedTrxDocument.MaximumDepth)
            + Repeat("</n>", BoundedTrxDocument.MaximumDepth) + "</Extra>",
        "nodes" => "<Extra>" + Repeat("<n/>", BoundedTrxDocument.MaximumNodeUnits) + "</Extra>",
        "attributes" => "<Extra>" + Repeat("<n a=\"\"/>", BoundedTrxDocument.MaximumNodeUnits / 2) + "</Extra>",
        _ => throw new ArgumentException("Unknown structural amplification.", nameof(kind)),
    };

    private static string Repeat(string value, int count)
    {
        StringBuilder builder = new(value.Length * count);
        for (int index = 0; index < count; index++)
        {
            builder.Append(value);
        }

        return builder.ToString();
    }

    private static string RecoveryTrx()
    {
        DateTimeOffset finish = DateTimeOffset.UtcNow;
        return $"""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Times start="{finish.AddSeconds(-1):O}" finish="{finish:O}" />
              <Results><UnitTestResult testId="recovery-1" outcome="Passed" /></Results>
              <TestDefinitions><UnitTest id="recovery-1"><TestMethod className="Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests" name="LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate" /></UnitTest></TestDefinitions>
              <ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" notExecuted="0" /></ResultSummary>
            </TestRun>
            """;
    }
}
