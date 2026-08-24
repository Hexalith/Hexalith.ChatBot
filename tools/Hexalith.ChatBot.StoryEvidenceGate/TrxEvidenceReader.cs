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
    private static readonly XNamespace TeamTestNamespace =
        "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    /// <summary>Reads and verifies one contract result lane.</summary>
    /// <param name="resultContract">The strict lane contract.</param>
    /// <param name="resultsRoot">The results root.</param>
    /// <param name="baseCommit">The exact expected base.</param>
    /// <param name="headCommit">The exact expected head.</param>
    /// <param name="implementationDigest">The exact expected digest.</param>
    /// <param name="repositoryIdentity">The policy-bound repository identity.</param>
    /// <param name="maximumCurrentRunAgeMinutes">The policy current-run age.</param>
    /// <param name="maximumRetainedEvidenceAgeHours">The policy retained-evidence age.</param>
    /// <param name="maximumFutureClockSkewMinutes">The policy future clock skew.</param>
    /// <param name="nowUtc">The evaluation clock.</param>
    /// <returns>The machine-derived lane result.</returns>
    public static LaneResult Read(
        JsonObject resultContract,
        string resultsRoot,
        string baseCommit,
        string headCommit,
        string implementationDigest,
        string repositoryIdentity,
        int maximumCurrentRunAgeMinutes,
        int maximumRetainedEvidenceAgeHours,
        int maximumFutureClockSkewMinutes,
        DateTimeOffset nowUtc)
    {
        ResultDefinition definition = ReadDefinition(resultContract);
        ValidateLocator(
            definition.Source,
            definition.ArtifactLocator,
            definition.TrxRelative,
            definition.ProvenanceRelative,
            repositoryIdentity,
            definition.Lane);
        string trxPath = ResolveSafeResultPath(resultsRoot, definition.TrxRelative);
        string provenancePath = ResolveSafeResultPath(resultsRoot, definition.ProvenanceRelative);
        byte[] trxBytes = ReadTrxBytes(trxPath, definition.Lane);
        string checksum = Convert.ToHexString(SHA256.HashData(trxBytes)).ToLowerInvariant();
        JsonObject provenance = EvidenceJson.LoadProvenance(provenancePath);
        DateTimeOffset producedAtUtc = VerifyProvenance(
            provenance,
            definition.Lane,
            definition.Source,
            definition.ArtifactLocator,
            definition.Selectors,
            repositoryIdentity,
            baseCommit,
            headCommit,
            implementationDigest,
            checksum,
            maximumCurrentRunAgeMinutes,
            maximumRetainedEvidenceAgeHours,
            maximumFutureClockSkewMinutes,
            nowUtc);
        return ValidateTrx(
            definition,
            trxBytes,
            checksum,
            producedAtUtc,
            maximumCurrentRunAgeMinutes,
            maximumRetainedEvidenceAgeHours,
            maximumFutureClockSkewMinutes,
            nowUtc);
    }

    /// <summary>Validates current-run TRX bytes before any provenance sidecar is created or replaced.</summary>
    internal static string PreflightCurrentRun(
        JsonObject resultContract,
        string resultsRoot,
        string repositoryIdentity,
        int maximumCurrentRunAgeMinutes,
        int maximumFutureClockSkewMinutes,
        DateTimeOffset producedAtUtc)
    {
        ResultDefinition definition = ReadDefinition(resultContract);
        if (!definition.Source.Equals("current-run", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, definition.Lane);
        }

        ValidateLocator(
            definition.Source,
            definition.ArtifactLocator,
            definition.TrxRelative,
            definition.ProvenanceRelative,
            repositoryIdentity,
            definition.Lane);
        string trxPath = ResolveSafeResultPath(resultsRoot, definition.TrxRelative);
        _ = ResolveSafeResultPath(resultsRoot, definition.ProvenanceRelative);
        byte[] trxBytes = ReadTrxBytes(trxPath, definition.Lane);
        string checksum = Convert.ToHexString(SHA256.HashData(trxBytes)).ToLowerInvariant();
        _ = ValidateTrx(
            definition,
            trxBytes,
            checksum,
            producedAtUtc,
            maximumCurrentRunAgeMinutes,
            maximumRetainedEvidenceAgeHours: 0,
            maximumFutureClockSkewMinutes,
            producedAtUtc);
        return checksum;
    }

    /// <summary>
    /// Validates the complete producer-owned lane grammar without reading result or provenance bytes.
    /// </summary>
    internal static void PreflightDefinition(
        JsonObject resultContract,
        string resultsRoot,
        string repositoryIdentity)
    {
        ResultDefinition definition = ReadDefinition(resultContract);
        ValidateLocator(
            definition.Source,
            definition.ArtifactLocator,
            definition.TrxRelative,
            definition.ProvenanceRelative,
            repositoryIdentity,
            definition.Lane);
        _ = ResolveSafeResultPath(resultsRoot, definition.TrxRelative);
        _ = ResolveSafeResultPath(resultsRoot, definition.ProvenanceRelative);
    }

    private static LaneResult ValidateTrx(
        ResultDefinition definition,
        byte[] trxBytes,
        string checksum,
        DateTimeOffset producedAtUtc,
        int maximumCurrentRunAgeMinutes,
        int maximumRetainedEvidenceAgeHours,
        int maximumFutureClockSkewMinutes,
        DateTimeOffset nowUtc)
    {
        string lane = definition.Lane;

        XDocument document;
        try
        {
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using MemoryStream stream = new(trxBytes, writable: false);
            using XmlReader reader = XmlReader.Create(stream, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        XElement root = document.Root is { } candidate
            && candidate.Name == TeamTestNamespace + "TestRun"
                ? candidate
                : throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        RejectForeignStructuralElements(document, lane);
        XElement times = SingleDirectChild(root, "Times", lane);
        XElement results = SingleDirectChild(root, "Results", lane);
        XElement testDefinitions = SingleDirectChild(root, "TestDefinitions", lane);
        XElement summary = SingleDirectChild(root, "ResultSummary", lane);
        XElement counters = SingleDirectChild(summary, "Counters", lane);
        string summaryOutcome = Attribute(summary, "outcome");
        if (!(summaryOutcome.Equals("Passed", StringComparison.OrdinalIgnoreCase)
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
        XElement[] resultElements = results.Elements(TeamTestNamespace + "UnitTestResult").ToArray();
        if (resultElements.Length != results.Elements().Count())
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        HashSet<string> resultIds = new(StringComparer.Ordinal);
        if (resultElements.Any(element => string.IsNullOrWhiteSpace(Attribute(element, "testId"))
                || !resultIds.Add(Attribute(element, "testId"))))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        Dictionary<string, string> testMethods = ReadTestMethods(testDefinitions, lane);
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
            || (!definition.AllowSkipped && skipped != 0)
            || executed != passed + failed
            || total != executed + skipped
            || resultElements.Length != total
            || resultPassed != passed
            || resultFailed != failed
            || resultSkipped != skipped
            || unknownOutcome
            || resultElements.Any(element => !testMethods.ContainsKey(Attribute(element, "testId"))))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        ValidateRunTimes(
            times,
            definition.Source,
            producedAtUtc,
            maximumCurrentRunAgeMinutes,
            maximumRetainedEvidenceAgeHours,
            maximumFutureClockSkewMinutes,
            nowUtc,
            lane);
        HashSet<string> passingTests = resultElements
            .Where(static element => Attribute(element, "outcome").Equals("Passed", StringComparison.OrdinalIgnoreCase))
            .Select(element => testMethods[Attribute(element, "testId")])
            .ToHashSet(StringComparer.Ordinal);
        if (definition.Selectors.Any(selector => !SelectorMatches(selector, passingTests)))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        return new LaneResult(
            lane,
            definition.PrimaryPathClass,
            definition.Source,
            total,
            executed,
            passed,
            failed,
            skipped,
            definition.ArtifactLocator,
            checksum,
            passingTests,
            definition.Selectors);
    }

    private static ResultDefinition ReadDefinition(JsonObject resultContract)
    {
        string lane = EvidenceJson.RequiredString(resultContract, "lane", GateReason.MachineResultsInvalid);
        IReadOnlyList<string> selectors = EvidenceJson.RequiredStrings(
            resultContract,
            "selectors",
            GateReason.MachineResultsInvalid);
        if (selectors.Count == 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        foreach (string selector in selectors)
        {
            string value = selector.StartsWith("class:", StringComparison.Ordinal)
                ? selector["class:".Length..]
                : selector.StartsWith("method:", StringComparison.Ordinal)
                    ? selector["method:".Length..]
                    : string.Empty;
            if (value.Length == 0
                || value.Any(static character => !(char.IsAsciiLetterOrDigit(character)
                    || character is '_' or '.' or '+' or '`'))
                || !(char.IsAsciiLetter(value[0]) || value[0] == '_'))
            {
                throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
            }
        }

        return new ResultDefinition(
            lane,
            EvidenceJson.RequiredString(resultContract, "source", GateReason.EvidenceStaleOrUnbound),
            EvidenceJson.RequiredString(resultContract, "artifactLocator", GateReason.EvidenceStaleOrUnbound),
            EvidenceJson.RequiredNullableString(resultContract, "primaryPathClass", GateReason.PrimaryPathNotExecuted),
            EvidenceJson.RequiredString(resultContract, "trx", GateReason.MachineResultsInvalid),
            EvidenceJson.RequiredString(resultContract, "provenance", GateReason.EvidenceStaleOrUnbound),
            EvidenceJson.RequiredBoolean(resultContract, "allowSkipped", GateReason.MachineResultsInvalid),
            selectors);
    }

    private static byte[] ReadTrxBytes(string trxPath, string lane)
    {
        try
        {
            return File.ReadAllBytes(trxPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }
    }

    private static void ValidateLocator(
        string source,
        string locator,
        string trxRelative,
        string provenanceRelative,
        string repositoryIdentity,
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

        string actualRepository = $"{match.Groups[1].Value}/{match.Groups[2].Value}";
        if (!actualRepository.Equals(repositoryIdentity, StringComparison.OrdinalIgnoreCase))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }

        string prefix = $"retained/{match.Groups[3].Value}/{match.Groups[4].Value}/";
        if (!trxRelative.StartsWith(prefix, StringComparison.Ordinal)
            || !provenanceRelative.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }
    }

    private static DateTimeOffset VerifyProvenance(
        JsonObject provenance,
        string lane,
        string source,
        string artifactLocator,
        IReadOnlyList<string> selectors,
        string repositoryIdentity,
        string baseCommit,
        string headCommit,
        string implementationDigest,
        string checksum,
        int maximumCurrentRunAgeMinutes,
        int maximumRetainedEvidenceAgeHours,
        int maximumFutureClockSkewMinutes,
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
            || producedAtUtc > nowUtc.AddMinutes(maximumFutureClockSkewMinutes)
            || nowUtc - producedAtUtc > (source.Equals("current-run", StringComparison.Ordinal)
                ? TimeSpan.FromMinutes(maximumCurrentRunAgeMinutes)
                : TimeSpan.FromHours(maximumRetainedEvidenceAgeHours)))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, lane);
        }

        IReadOnlyList<string> provenanceSelectors = EvidenceJson.RequiredStrings(
            provenance,
            "selectors",
            GateReason.EvidenceStaleOrUnbound);
        if (!EvidenceJson.RequiredString(provenance, "schemaVersion", GateReason.EvidenceStaleOrUnbound)
                .Equals("2.0", StringComparison.Ordinal)
            || !EvidenceJson.RequiredString(provenance, "repositoryIdentity", GateReason.EvidenceStaleOrUnbound)
                .Equals(repositoryIdentity, StringComparison.OrdinalIgnoreCase)
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

        return producedAtUtc;
    }

    private static Dictionary<string, string> ReadTestMethods(XElement testDefinitions, string lane)
    {
        Dictionary<string, string> methods = new(StringComparer.Ordinal);
        XElement[] unitTests = testDefinitions.Elements(TeamTestNamespace + "UnitTest").ToArray();
        if (unitTests.Length != testDefinitions.Elements().Count())
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        foreach (XElement unitTest in unitTests)
        {
            string id = Attribute(unitTest, "id");
            XElement[] testMethods = unitTest.Elements(TeamTestNamespace + "TestMethod").ToArray();
            if (testMethods.Length != 1)
            {
                throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
            }

            XElement testMethod = testMethods[0];
            string className = Attribute(testMethod, "className");
            string methodName = Attribute(testMethod, "name");
            if (string.IsNullOrWhiteSpace(id)
                || string.IsNullOrWhiteSpace(className)
                || string.IsNullOrWhiteSpace(methodName)
                || !methods.TryAdd(id, $"{className}.{methodName}"))
            {
                throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
            }
        }

        if (methods.Count == 0)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        return methods;
    }

    private static XElement SingleDirectChild(XElement parent, string localName, string lane)
    {
        XElement[] matches = parent.Elements(TeamTestNamespace + localName).ToArray();
        if (matches.Length != 1)
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }

        return matches[0];
    }

    private static void RejectForeignStructuralElements(XDocument document, string lane)
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
            throw new GateValidationException(GateReason.MachineResultsInvalid, lane);
        }
    }

    private static void ValidateRunTimes(
        XElement times,
        string source,
        DateTimeOffset producedAtUtc,
        int maximumCurrentRunAgeMinutes,
        int maximumRetainedEvidenceAgeHours,
        int maximumFutureClockSkewMinutes,
        DateTimeOffset nowUtc,
        string lane)
    {
        if (!DateTimeOffset.TryParse(
                Attribute(times, "start"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset startUtc)
            || !DateTimeOffset.TryParse(
                Attribute(times, "finish"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset finishUtc)
            || finishUtc < startUtc
            || finishUtc > nowUtc.AddMinutes(maximumFutureClockSkewMinutes)
            || producedAtUtc < finishUtc.AddMinutes(-maximumFutureClockSkewMinutes)
            || nowUtc - finishUtc > (source.Equals("current-run", StringComparison.Ordinal)
                ? TimeSpan.FromMinutes(maximumCurrentRunAgeMinutes)
                : TimeSpan.FromHours(maximumRetainedEvidenceAgeHours)))
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
            if (relativePath.Contains('\\', StringComparison.Ordinal))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path");
            }

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

    private sealed record ResultDefinition(
        string Lane,
        string Source,
        string ArtifactLocator,
        string? PrimaryPathClass,
        string TrxRelative,
        string ProvenanceRelative,
        bool AllowSkipped,
        IReadOnlyList<string> Selectors);
}
