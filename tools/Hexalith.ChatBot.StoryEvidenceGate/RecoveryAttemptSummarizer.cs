using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Projects any live-recovery producer attempt into a metadata-only summary that survives a failed drill.
/// </summary>
/// <remarks>
/// <see cref="RecoveryTrxSanitizer"/> deliberately admits only a clean passing run, because its output feeds the
/// completion evidence path. That left a failed multi-hour drill with no retained machine evidence at all. This
/// summarizer is the ADR ADV-3 failure record: it never throws on a bad or absent TRX, and it copies only
/// counters, clock bounds and test identity -- never captured standard output, messages or stack traces, which
/// are the channel through which tenant payloads would escape into a CI artifact.
/// </remarks>
public static class RecoveryAttemptSummarizer
{
    private static readonly XNamespace TeamTestNamespace =
        "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    private static readonly string[] CounterNames =
    [
        "total", "executed", "passed", "failed", "error", "timeout", "aborted", "inconclusive",
        "notExecuted", "notRunnable", "warning", "passedButRunAborted", "disconnected", "pending",
    ];

    /// <summary>Writes a metadata-only record of one recovery producer attempt.</summary>
    /// <param name="inputPath">The raw producer TRX path, which may not exist.</param>
    /// <param name="producerOutcome">The workflow-reported step outcome.</param>
    /// <param name="outputPath">The summary output path.</param>
    /// <param name="policy">The metadata-only policy governing the retained summary.</param>
    public static void Summarize(
        string inputPath,
        string producerOutcome,
        string outputPath,
        JsonObject policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(policy);
        JsonObject metadataPolicy = EvidenceJson.RequiredObject(
            policy,
            "metadataOnly",
            GateReason.EvidencePayloadForbidden);
        int maximumStringLength = EvidenceJson.RequiredInteger(
            metadataPolicy,
            "maximumStringLength",
            GateReason.EvidencePayloadForbidden);
        IReadOnlyList<string> forbiddenTokenFragments = EvidenceJson.RequiredStrings(
            metadataPolicy,
            "forbiddenFieldNames",
            GateReason.EvidencePayloadForbidden);
        JsonObject summary = new()
        {
            ["schemaVersion"] = "1.0",
            ["kind"] = "live-recovery-attempt-summary",
            ["producerOutcome"] = NormalizeOutcome(producerOutcome),
        };

        bool inputPresent = !string.IsNullOrWhiteSpace(inputPath) && File.Exists(inputPath);
        XDocument? document = TryLoad(inputPath);
        if (document?.Root is not { } root || root.Name != TeamTestNamespace + "TestRun")
        {
            summary["trxPresent"] = inputPresent;
            summary["trxState"] = inputPresent ? "malformed" : "absent";
            TryWrite(outputPath, summary, policy);
            return;
        }

        summary["trxPresent"] = true;
        summary["trxState"] = "parsed";
        if (root.Element(TeamTestNamespace + "Times") is { } times)
        {
            summary["startedAtUtc"] = SafeTimestamp(times.Attribute("start")?.Value);
            summary["finishedAtUtc"] = SafeTimestamp(times.Attribute("finish")?.Value);
        }

        XElement? resultSummary = root.Element(TeamTestNamespace + "ResultSummary");
        summary["runOutcome"] = SafeToken(
            resultSummary?.Attribute("outcome")?.Value,
            maximumStringLength,
            forbiddenTokenFragments);

        JsonObject counters = [];
        XElement? counterElement = resultSummary?.Element(TeamTestNamespace + "Counters");
        foreach (string name in CounterNames)
        {
            counters[name] = counterElement?.Attribute(name)?.Value is { } raw
                && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                    ? value
                    : null;
        }

        summary["counters"] = counters;

        XElement[] methods = root.Descendants(TeamTestNamespace + "TestMethod").ToArray();
        summary["testMethodCount"] = methods.Length;
        summary["testsTruncated"] = methods.Length > 32;
        JsonArray tests = [];
        foreach (XElement method in methods.Take(32))
        {
            tests.Add(new JsonObject
            {
                ["className"] = SafeToken(
                    method.Attribute("className")?.Value,
                    maximumStringLength,
                    forbiddenTokenFragments),
                ["name"] = SafeToken(
                    method.Attribute("name")?.Value,
                    maximumStringLength,
                    forbiddenTokenFragments),
            });
        }

        summary["tests"] = tests;
        TryWrite(outputPath, summary, policy);
    }

    private static XDocument? TryLoad(string inputPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            {
                return null;
            }

            if (new FileInfo(inputPath).Length > RecoveryTrxSanitizer.MaximumTrxBytes)
            {
                return null;
            }

            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = RecoveryTrxSanitizer.MaximumTrxCharacters,
            };
            using FileStream stream = File.OpenRead(inputPath);
            using XmlReader reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception exception) when (exception is XmlException
            or IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or OutOfMemoryException)
        {
            return null;
        }
    }

    private static string NormalizeOutcome(string? producerOutcome) =>
        producerOutcome is "success" or "failure" or "cancelled" or "skipped"
            ? producerOutcome
            : "unknown";

    // Timestamps and identifiers are copied only when they match a conservative shape, so a hostile or corrupt
    // TRX cannot smuggle free text into the artifact through an attribute the summary echoes.
    private static JsonNode? SafeTimestamp(string? value) =>
        value is not null
            && DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset parsed)
            ? JsonValue.Create(parsed.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
            : null;

    private static JsonNode? SafeToken(
        string? value,
        int maximumStringLength,
        IReadOnlyList<string> forbiddenFragments) =>
        value is not null
            && value.Length <= maximumStringLength
            && value.All(static character => char.IsAsciiLetterOrDigit(character)
                || character is '.' or '_' or '-' or '+' or '`')
            && !forbiddenFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            ? JsonValue.Create(value)
            : null;

    private static void TryWrite(string outputPath, JsonObject summary, JsonObject policy)
    {
        try
        {
            EvidenceJson.ValidateMetadataOnly(summary, policy);
            string fullPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)
                ?? throw new InvalidOperationException("Recovery attempt summary output has no parent directory."));
            File.WriteAllText(
                fullPath,
                summary.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception) when (exception is GateValidationException
            or IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException)
        {
            // Best effort by contract: retaining no summary must never replace the producer failure being reported.
        }
    }
}
