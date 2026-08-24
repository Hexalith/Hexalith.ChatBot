using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Projects the raw live-recovery TRX into the metadata-only shape admitted to completion evidence.
/// </summary>
public static class RecoveryTrxSanitizer
{
    private const string ExpectedClass =
        "Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests";
    private const string ExpectedMethod =
        "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate";
    private static readonly XNamespace TeamTestNamespace =
        "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    /// <summary>Writes a deterministic payload-free projection of one passing recovery result.</summary>
    public static void Sanitize(string inputPath, string outputPath)
    {
        XDocument source;
        try
        {
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using FileStream stream = File.OpenRead(inputPath);
            using XmlReader reader = XmlReader.Create(stream, settings);
            source = XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception exception) when (exception is XmlException
            or IOException
            or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "recovery-sanitize");
        }

        XElement root = source.Root is { } candidate && candidate.Name == TeamTestNamespace + "TestRun"
            ? candidate
            : throw new GateValidationException(GateReason.MachineResultsInvalid, "recovery-sanitize");
        RejectForeignStructuralElements(source);
        XElement times = Single(root, "Times");
        XElement results = Single(root, "Results");
        XElement definitions = Single(root, "TestDefinitions");
        XElement summary = Single(root, "ResultSummary");
        XElement counters = Single(summary, "Counters");
        XElement result = Single(results, "UnitTestResult");
        XElement definition = Single(definitions, "UnitTest");
        XElement method = Single(definition, "TestMethod");

        string testId = Attribute(result, "testId");
        string outcome = Attribute(result, "outcome");
        string summaryOutcome = Attribute(summary, "outcome");
        bool validTimes = DateTimeOffset.TryParse(
                Attribute(times, "start"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset startedAt)
            && DateTimeOffset.TryParse(
                Attribute(times, "finish"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset finishedAt)
            && finishedAt >= startedAt;
        if (!Regex.IsMatch(testId, "^[A-Za-z0-9_.-]{1,128}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)
            || !Attribute(definition, "id").Equals(testId, StringComparison.Ordinal)
            || !outcome.Equals("Passed", StringComparison.OrdinalIgnoreCase)
            || !(summaryOutcome.Equals("Passed", StringComparison.OrdinalIgnoreCase)
                || summaryOutcome.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            || !Attribute(method, "className").Equals(ExpectedClass, StringComparison.Ordinal)
            || !Attribute(method, "name").Equals(ExpectedMethod, StringComparison.Ordinal)
            || !validTimes
            || Counter(counters, "total") != 1
            || Counter(counters, "executed") != 1
            || Counter(counters, "passed") != 1
            || Counter(counters, "failed") != 0
            || Counter(counters, "error") != 0
            || Counter(counters, "timeout") != 0
            || Counter(counters, "aborted") != 0
            || Counter(counters, "inconclusive") != 0
            || Counter(counters, "notExecuted") != 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "recovery-sanitize");
        }

        string fullOutput = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)
            ?? throw new InvalidOperationException("Sanitized TRX output has no parent directory."));
        string canonicalName = $"{ExpectedClass}.{ExpectedMethod}";
        XDocument sanitized = new(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                TeamTestNamespace + "TestRun",
                new XElement(
                    TeamTestNamespace + "Times",
                    new XAttribute("start", Attribute(times, "start")),
                    new XAttribute("finish", Attribute(times, "finish"))),
                new XElement(
                    TeamTestNamespace + "Results",
                    new XElement(
                        TeamTestNamespace + "UnitTestResult",
                        new XAttribute("testId", testId),
                        new XAttribute("testName", canonicalName),
                        new XAttribute("outcome", "Passed"))),
                new XElement(
                    TeamTestNamespace + "TestDefinitions",
                    new XElement(
                        TeamTestNamespace + "UnitTest",
                        new XAttribute("id", testId),
                        new XAttribute("name", canonicalName),
                        new XElement(
                            TeamTestNamespace + "TestMethod",
                            new XAttribute("className", ExpectedClass),
                            new XAttribute("name", ExpectedMethod)))),
                new XElement(
                    TeamTestNamespace + "ResultSummary",
                    new XAttribute("outcome", "Completed"),
                    new XElement(
                        TeamTestNamespace + "Counters",
                        new XAttribute("total", "1"),
                        new XAttribute("executed", "1"),
                        new XAttribute("passed", "1"),
                        new XAttribute("failed", "0"),
                        new XAttribute("error", "0"),
                        new XAttribute("timeout", "0"),
                        new XAttribute("aborted", "0"),
                        new XAttribute("inconclusive", "0"),
                        new XAttribute("notExecuted", "0")))));
        sanitized.Save(fullOutput, SaveOptions.DisableFormatting);
    }

    private static XElement Single(XElement parent, string localName)
    {
        XElement[] elements = parent.Elements(TeamTestNamespace + localName).ToArray();
        if (elements.Length != 1 || parent.Elements().Count(element => element.Name == TeamTestNamespace + localName) != 1)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "recovery-sanitize");
        }

        return elements[0];
    }

    private static void RejectForeignStructuralElements(XDocument document)
    {
        HashSet<string> structuralNames =
        [
            "TestRun",
            "Times",
            "Results",
            "UnitTestResult",
            "TestDefinitions",
            "UnitTest",
            "TestMethod",
            "ResultSummary",
            "Counters",
        ];
        if (document.Descendants().Any(element => structuralNames.Contains(element.Name.LocalName)
                && element.Name.Namespace != TeamTestNamespace))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, "recovery-sanitize");
        }
    }

    private static int Counter(XElement counters, string name) =>
        int.TryParse(Attribute(counters, name), NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            ? value
            : -1;

    private static string Attribute(XElement element, string name) =>
        element.Attribute(name)?.Value ?? string.Empty;
}
