using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Produces checksum and exact-scope provenance sidecars for existing TRX machine results.
/// </summary>
public static class ProvenanceAttestor
{
    internal sealed record AttestationLane(JsonObject Contract, string Checksum);

    internal sealed record AttestationPlan(
        string StoryKey,
        string ResultsRoot,
        string BaseCommit,
        string HeadCommit,
        string ImplementationDigest,
        string RepositoryIdentity,
        DateTimeOffset ProducedAtUtc,
        IReadOnlyList<AttestationLane> CurrentRunLanes,
        IReadOnlyList<string> ResultPaths);

    /// <summary>Attests every declared lane whose TRX exists under the results root.</summary>
    /// <param name="repositoryRoot">The root repository.</param>
    /// <param name="contractPath">The evidence contract.</param>
    /// <param name="baseCommit">The exact base revision.</param>
    /// <param name="headCommit">The exact head revision.</param>
    /// <param name="resultsRoot">The results root.</param>
    /// <param name="producedAtUtc">The production timestamp.</param>
    /// <param name="policyPath">Optional policy path; defaults to the repository-root policy file.</param>
    public static void AttestContract(
        string repositoryRoot,
        string contractPath,
        string baseCommit,
        string headCommit,
        string resultsRoot,
        DateTimeOffset producedAtUtc,
        string? policyPath = null)
    {
        AttestationPlan plan = PreflightContract(
            repositoryRoot,
            contractPath,
            baseCommit,
            headCommit,
            resultsRoot,
            producedAtUtc,
            policyPath);
        WritePlan(plan);
    }

    internal static AttestationPlan PreflightContract(
        string repositoryRoot,
        string contractPath,
        string baseCommit,
        string headCommit,
        string resultsRoot,
        DateTimeOffset producedAtUtc,
        string? policyPath = null)
    {
        ArgumentNullException.ThrowIfNull(baseCommit);
        ArgumentNullException.ThrowIfNull(headCommit);
        JsonObject policy = EvidenceJson.LoadPolicy(
            policyPath ?? Path.Combine(repositoryRoot, "story-evidence-policy.json"));
        JsonObject contract = EvidenceJson.LoadContract(contractPath);
        StoryEvidenceValidator.ValidatePinnedPolicy(policy);
        StoryEvidenceValidator.ValidateAttestationContract(contract);
        ScopeEvaluation scope = ScopeEvaluator.Evaluate(repositoryRoot, policy, contract, baseCommit, headCommit);
        JsonArray results = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
        string repositoryIdentity = EvidenceJson.RequiredString(
            policy,
            "repositoryIdentity",
            GateReason.EvidenceStaleOrUnbound);
        int maximumFutureClockSkewMinutes = EvidenceJson.RequiredInteger(
            policy,
            "maximumFutureClockSkewMinutes",
            GateReason.EvidenceStaleOrUnbound);
        List<AttestationLane> currentRunLanes = [];
        List<string> resultPaths = [];
        HashSet<string> uniqueResultPaths = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonNode? node in results)
        {
            JsonObject lane = node as JsonObject
                ?? throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
            string trxPath = TrxEvidenceReader.ResolveSafeResultPath(
                resultsRoot,
                EvidenceJson.RequiredString(lane, "trx", GateReason.MachineResultsInvalid));
            string provenancePath = TrxEvidenceReader.ResolveSafeResultPath(
                resultsRoot,
                EvidenceJson.RequiredString(lane, "provenance", GateReason.EvidenceStaleOrUnbound));
            if (!uniqueResultPaths.Add(trxPath) || !uniqueResultPaths.Add(provenancePath))
            {
                throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path-collision");
            }

            resultPaths.Add(trxPath);
            resultPaths.Add(provenancePath);
            if (!EvidenceJson.RequiredString(lane, "source", GateReason.EvidenceStaleOrUnbound)
                    .Equals("current-run", StringComparison.Ordinal))
            {
                continue;
            }

            string checksum = TrxEvidenceReader.PreflightCurrentRun(
                lane,
                resultsRoot,
                repositoryIdentity,
                EvidenceJson.ResolveCurrentRunAgeMinutes(
                    policy,
                    EvidenceJson.RequiredString(lane, "lane", GateReason.MachineResultsInvalid)),
                maximumFutureClockSkewMinutes,
                producedAtUtc);
            currentRunLanes.Add(new AttestationLane(lane, checksum));
        }

        return new AttestationPlan(
            EvidenceJson.RequiredStoryKey(contract),
            resultsRoot,
            baseCommit,
            headCommit,
            scope.Digest,
            repositoryIdentity,
            producedAtUtc,
            currentRunLanes,
            resultPaths);
    }

    internal static void WritePlan(AttestationPlan plan)
    {
        foreach (AttestationLane lane in plan.CurrentRunLanes)
        {
            WriteLane(
                lane.Contract,
                plan.ResultsRoot,
                plan.BaseCommit,
                plan.HeadCommit,
                plan.ImplementationDigest,
                plan.RepositoryIdentity,
                plan.ProducedAtUtc,
                lane.Checksum);
        }
    }

    private static void WriteLane(
        JsonObject lane,
        string resultsRoot,
        string baseCommit,
        string headCommit,
        string implementationDigest,
        string repositoryIdentity,
        DateTimeOffset producedAtUtc,
        string checksum)
    {
        string trxRelative = EvidenceJson.RequiredString(lane, "trx", GateReason.MachineResultsInvalid);
        string provenanceRelative = EvidenceJson.RequiredString(
            lane,
            "provenance",
            GateReason.EvidenceStaleOrUnbound);
        string artifactLocator = EvidenceJson.RequiredString(
            lane,
            "artifactLocator",
            GateReason.EvidenceStaleOrUnbound);
        if (!artifactLocator.Equals($"file:{trxRelative}", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "artifact-locator");
        }

        string trxPath = TrxEvidenceReader.ResolveSafeResultPath(resultsRoot, trxRelative);
        string provenancePath = TrxEvidenceReader.ResolveSafeResultPath(resultsRoot, provenanceRelative);
        if (!File.Exists(trxPath))
        {
            throw new GateValidationException(GateReason.MachineResultsInvalid, trxRelative);
        }

        JsonObject sidecar = new()
        {
            ["schemaVersion"] = "2.0",
            ["repositoryIdentity"] = repositoryIdentity,
            ["baseCommit"] = baseCommit.ToLowerInvariant(),
            ["headCommit"] = headCommit.ToLowerInvariant(),
            ["implementationDigest"] = implementationDigest,
            ["trxSha256"] = checksum,
            ["lane"] = EvidenceJson.RequiredString(lane, "lane", GateReason.MachineResultsInvalid),
            ["source"] = "current-run",
            ["selectors"] = JsonSerializer.SerializeToNode(
                EvidenceJson.RequiredStrings(lane, "selectors", GateReason.MachineResultsInvalid),
                JsonReportWriter.SerializerOptions),
            ["producedAtUtc"] = producedAtUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["artifactLocator"] = artifactLocator,
        };
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(provenancePath)
                ?? throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path"));
            _ = TrxEvidenceReader.ResolveSafeResultPath(resultsRoot, provenanceRelative);
            File.WriteAllText(provenancePath, sidecar.ToJsonString(JsonReportWriter.SerializerOptions));
        }
        catch (GateValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path");
        }
    }
}
