using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Produces checksum and exact-scope provenance sidecars for existing TRX machine results.
/// </summary>
public static class ProvenanceAttestor
{
    /// <summary>Attests every declared lane whose TRX exists under the results root.</summary>
    /// <param name="repositoryRoot">The root repository.</param>
    /// <param name="contractPath">The evidence contract.</param>
    /// <param name="baseCommit">The exact base revision.</param>
    /// <param name="headCommit">The exact head revision.</param>
    /// <param name="resultsRoot">The results root.</param>
    /// <param name="producedAtUtc">The production timestamp.</param>
    public static void AttestContract(
        string repositoryRoot,
        string contractPath,
        string baseCommit,
        string headCommit,
        string resultsRoot,
        DateTimeOffset producedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(baseCommit);
        ArgumentNullException.ThrowIfNull(headCommit);
        JsonObject policy = EvidenceJson.LoadPolicy(Path.Combine(repositoryRoot, "story-evidence-policy.json"));
        JsonObject contract = EvidenceJson.LoadContract(contractPath);
        ScopeEvaluation scope = ScopeEvaluator.Evaluate(repositoryRoot, policy, contract, baseCommit, headCommit);
        JsonArray results = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
        foreach (JsonNode? node in results)
        {
            JsonObject lane = node as JsonObject
                ?? throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
            if (!EvidenceJson.RequiredString(lane, "source", GateReason.EvidenceStaleOrUnbound)
                    .Equals("current-run", StringComparison.Ordinal))
            {
                continue;
            }

            WriteLane(lane, resultsRoot, baseCommit, headCommit, scope.Digest, producedAtUtc);
        }
    }

    private static void WriteLane(
        JsonObject lane,
        string resultsRoot,
        string baseCommit,
        string headCommit,
        string implementationDigest,
        DateTimeOffset producedAtUtc)
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
            ["schemaVersion"] = "1.0",
            ["baseCommit"] = baseCommit.ToLowerInvariant(),
            ["headCommit"] = headCommit.ToLowerInvariant(),
            ["implementationDigest"] = implementationDigest,
            ["trxSha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(trxPath))).ToLowerInvariant(),
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
