using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Parses non-vacuous TRX machine results and verifies their provenance sidecars.
/// </summary>
public static class TrxEvidenceReader
{
    /// <summary>Reads and verifies one contract result lane.</summary>
    /// <param name="resultContract">The strict lane contract.</param>
    /// <param name="resultsRoot">The results root.</param>
    /// <param name="baseCommit">The exact expected base.</param>
    /// <param name="headCommit">The exact expected head.</param>
    /// <param name="implementationDigest">The exact expected digest.</param>
    /// <param name="maximumAgeHours">The policy maximum age.</param>
    /// <param name="nowUtc">The evaluation clock.</param>
    /// <returns>The machine-derived lane result.</returns>
    public static LaneResult Read(
        JsonObject resultContract,
        string resultsRoot,
        string baseCommit,
        string headCommit,
        string implementationDigest,
        int maximumAgeHours,
        DateTimeOffset nowUtc)
    {
        string lane = EvidenceJson.RequiredString(resultContract, "lane", GateReason.MachineResultsInvalid);
        string source = EvidenceJson.RequiredString(resultContract, "source", GateReason.EvidenceStaleOrUnbound);
        string artifactLocator = EvidenceJson.RequiredString(
            resultContract,
            "artifactLocator",
            GateReason.EvidenceStaleOrUnbound);
        string? primaryPathClass = EvidenceJson.RequiredNullableString(
            resultContract,
            "primaryPathClass",
            GateReason.PrimaryPathNotExecuted);
        string trxRelative = EvidenceJson.RequiredString(resultContract, "trx", GateReason.MachineResultsInvalid);
        string provenanceRelative = EvidenceJson.RequiredString(
            resultContract,
            "provenance",
            GateReason.EvidenceStaleOrUnbound);
        bool allowSkipped = EvidenceJson.RequiredBoolean(
            resultContract,
            "allowSkipped",
            GateReason.MachineResultsInvalid);
        IReadOnlyList<string> selectors = EvidenceJson.RequiredStrings(
            resultContract,
            "selectors",
            GateReason.MachineResultsInvalid);
        if (selectors.Count == 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        ValidateLocator(source, artifactLocator, trxRelative, provenanceRelative, lane);

        string trxPath = ResolveSafeResultPath(resultsRoot, trxRelative);
        string provenancePath = ResolveSafeResultPath(resultsRoot, provenanceRelative);
        if (!File.Exists(trxPath))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        string checksum;
        try
        {
            checksum = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(trxPath))).ToLowerInvariant();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }
        JsonObject provenance = EvidenceJson.LoadProvenance(provenancePath);
        VerifyProvenance(
            provenance,
            lane,
            source,
            artifactLocator,
            selectors,
            baseCommit,
            headCommit,
            implementationDigest,
            checksum,
            maximumAgeHours,
            nowUtc);

        XDocument document;
        try
        {
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using XmlReader reader = XmlReader.Create(trxPath, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        XElement? summary = document.Descendants().FirstOrDefault(static element => element.Name.LocalName == "ResultSummary");
        XElement? counters = document.Descendants().FirstOrDefault(static element => element.Name.LocalName == "Counters");
        string summaryOutcome = summary is null ? string.Empty : Attribute(summary, "outcome");
        if (summary is null
            || counters is null
            || !(summaryOutcome.Equals("Passed", StringComparison.OrdinalIgnoreCase)
                || summaryOutcome.Equals("Completed", StringComparison.OrdinalIgnoreCase)))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        int total = Counter(counters, "total", lane);
        int executed = Counter(counters, "executed", lane);
        int passed = Counter(counters, "passed", lane);
        int failed = Counter(counters, "failed", lane)
            + Counter(counters, "error", lane)
            + Counter(counters, "timeout", lane)
            + Counter(counters, "aborted", lane);
        int skipped = Counter(counters, "notExecuted", lane) + Counter(counters, "inconclusive", lane);
        XElement[] resultElements = document.Descendants()
            .Where(static element => element.Name.LocalName == "UnitTestResult")
            .ToArray();
        int resultPassed = resultElements.Count(element =>
            Attribute(element, "outcome").Equals("Passed", StringComparison.OrdinalIgnoreCase));
        int resultFailed = resultElements.Count(element => IsFailedOutcome(Attribute(element, "outcome")));
        int resultSkipped = resultElements.Count(element => IsSkippedOutcome(Attribute(element, "outcome")));
        bool unknownOutcome = resultElements.Any(element =>
            !Attribute(element, "outcome").Equals("Passed", StringComparison.OrdinalIgnoreCase)
            && !IsFailedOutcome(Attribute(element, "outcome"))
            && !IsSkippedOutcome(Attribute(element, "outcome")));
        if (total <= 0
            || executed <= 0
            || passed <= 0
            || failed != 0
            || (!allowSkipped && skipped != 0)
            || executed != passed + failed
            || total != executed + skipped
            || resultElements.Length != total
            || resultPassed != passed
            || resultFailed != failed
            || resultSkipped != skipped
            || unknownOutcome)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        HashSet<string> passingTests = resultElements
            .Where(static element => Attribute(element, "outcome").Equals("Passed", StringComparison.OrdinalIgnoreCase))
            .Select(static element => Attribute(element, "testName"))
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);
        if (selectors.Any(selector => !SelectorMatches(selector, passingTests)))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        return new LaneResult(
            lane,
            primaryPathClass,
            source,
            total,
            executed,
            passed,
            failed,
            skipped,
            artifactLocator,
            checksum,
            passingTests,
            selectors);
    }

    private static void ValidateLocator(
        string source,
        string locator,
        string trxRelative,
        string provenanceRelative,
        string lane)
    {
        if (source.Equals("current-run", StringComparison.Ordinal))
        {
            if (!locator.Equals($"file:{trxRelative}", StringComparison.Ordinal))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
            }

            return;
        }

        if (!source.Equals("retained", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }

        Match match = Regex.Match(
            locator,
            "^github-actions://([A-Za-z0-9_.-]+)/([A-Za-z0-9_.-]+)/runs/([1-9][0-9]{0,19})/artifacts/([A-Za-z0-9][A-Za-z0-9_.-]{0,127})$",
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        if (!match.Success)
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }

        string? expectedRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        if (!string.IsNullOrWhiteSpace(expectedRepository))
        {
            string actualRepository = $"{match.Groups[1].Value}/{match.Groups[2].Value}";
            if (!actualRepository.Equals(expectedRepository, StringComparison.OrdinalIgnoreCase))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
            }
        }

        string prefix = $"retained/{match.Groups[3].Value}/{match.Groups[4].Value}/";
        if (!trxRelative.StartsWith(prefix, StringComparison.Ordinal)
            || !provenanceRelative.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }
    }

    private static void VerifyProvenance(
        JsonObject provenance,
        string lane,
        string source,
        string artifactLocator,
        IReadOnlyList<string> selectors,
        string baseCommit,
        string headCommit,
        string implementationDigest,
        string checksum,
        int maximumAgeHours,
        DateTimeOffset nowUtc)
    {
        string producedText = EvidenceJson.RequiredString(
            provenance,
            "producedAtUtc",
            GateReason.EvidenceStaleOrUnbound);
        if (!DateTimeOffset.TryParse(
                producedText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset producedAtUtc)
            || producedAtUtc > nowUtc.AddMinutes(5)
            || nowUtc - producedAtUtc > TimeSpan.FromHours(maximumAgeHours))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }

        IReadOnlyList<string> provenanceSelectors = EvidenceJson.RequiredStrings(
            provenance,
            "selectors",
            GateReason.EvidenceStaleOrUnbound);
        if (!EvidenceJson.RequiredString(provenance, "schemaVersion", GateReason.EvidenceStaleOrUnbound)
                .Equals("1.0", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(provenance, "lane", GateReason.EvidenceStaleOrUnbound)
                .Equals(lane, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(provenance, "source", GateReason.EvidenceStaleOrUnbound)
                .Equals(source, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(provenance, "artifactLocator", GateReason.EvidenceStaleOrUnbound)
                .Equals(artifactLocator, StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(provenance, "baseCommit", GateReason.EvidenceStaleOrUnbound)
                .Equals(baseCommit, StringComparison.OrdinalIgnoreCase)
            || !EvidenceJson.RequiredString(provenance, "headCommit", GateReason.EvidenceStaleOrUnbound)
                .Equals(headCommit, StringComparison.OrdinalIgnoreCase)
            || !EvidenceJson.RequiredString(provenance, "implementationDigest", GateReason.EvidenceStaleOrUnbound)
                .Equals(implementationDigest, StringComparison.OrdinalIgnoreCase)
            || !EvidenceJson.RequiredString(provenance, "trxSha256", GateReason.EvidenceStaleOrUnbound)
                .Equals(checksum, StringComparison.OrdinalIgnoreCase)
            || !provenanceSelectors.SequenceEqual(selectors, StringComparer.Ordinal))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }
    }

    private static bool SelectorMatches(string selector, IReadOnlySet<string> passingTests)
    {
        const string ClassPrefix = "class:";
        const string MethodPrefix = "method:";
        if (selector.StartsWith(ClassPrefix, StringComparison.Ordinal))
        {
            string className = selector[ClassPrefix.Length..];
            return className.Length > 0
                && passingTests.Any(name => name.StartsWith(className + ".", StringComparison.Ordinal));
        }

        if (selector.StartsWith(MethodPrefix, StringComparison.Ordinal))
        {
            string methodName = selector[MethodPrefix.Length..];
            return methodName.Length > 0
                && passingTests.Any(name => name.Equals(methodName, StringComparison.Ordinal)
                    || name.StartsWith(methodName + "(", StringComparison.Ordinal));
        }

        return false;
    }

    internal static string ResolveSafeResultPath(string resultsRoot, string relativePath)
    {
        try
        {
            string normalizedRoot = Path.GetFullPath(resultsRoot).TrimEnd(Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
            if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || Path.IsPathRooted(relativePath)
                || relativePath.Replace('\\', '/').Split('/').Any(
                    static segment => segment.Length == 0 || segment is "." or ".."))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path");
            }

            for (DirectoryInfo? ancestor = new(normalizedRoot); ancestor is not null; ancestor = ancestor.Parent)
            {
                if (ancestor.LinkTarget is not null)
                {
                    throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path");
                }
            }

            string current = normalizedRoot;
            foreach (string segment in Path.GetRelativePath(normalizedRoot, fullPath)
                         .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (new DirectoryInfo(current).LinkTarget is not null
                    || new FileInfo(current).LinkTarget is not null)
                {
                    throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path");
                }
            }

            return fullPath;
        }
        catch (GateValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException)
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path");
        }
    }

    private static int Counter(XElement counters, string name, string lane)
    {
        if (!int.TryParse(Attribute(counters, name), NumberStyles.None, CultureInfo.InvariantCulture, out int value)
            || value < 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        return value;
    }

    private static bool IsFailedOutcome(string outcome) => outcome.Equals("Failed", StringComparison.OrdinalIgnoreCase)
        || outcome.Equals("Error", StringComparison.OrdinalIgnoreCase)
        || outcome.Equals("Timeout", StringComparison.OrdinalIgnoreCase)
        || outcome.Equals("Aborted", StringComparison.OrdinalIgnoreCase);

    private static bool IsSkippedOutcome(string outcome) => outcome.Equals("NotExecuted", StringComparison.OrdinalIgnoreCase)
        || outcome.Equals("Inconclusive", StringComparison.OrdinalIgnoreCase);

    private static string Attribute(XElement element, string name) =>
        element.Attribute(name)?.Value ?? string.Empty;
}
