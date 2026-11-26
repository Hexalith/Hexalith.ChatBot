using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Validates completion contracts before any optional destructive producer is started.
/// </summary>
public static class CompletionProductionPlanner
{
    private const string RecoveryLane = "recovery-primary";
    private const string RecoveryClass = "recovery";
    private const string RecoverySelector =
        "class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests";
    private const string RecoveryTrx = "recovery-primary/live-recovery-validation.trx";
    private const string RecoveryProvenance = "recovery-primary/live-recovery-validation.provenance.json";

    /// <summary>Builds a fail-closed producer plan from exact active contracts.</summary>
    public static CompletionProductionPlan Plan(
        string repositoryRoot,
        string policyPath,
        string baseCommit,
        string headCommit,
        string resultsRoot)
    {
        JsonObject policy = EvidenceJson.LoadPolicy(policyPath);
        StoryEvidenceValidator.ValidatePinnedPolicy(policy);
        IReadOnlyList<TransitionRecord> transitions = TransitionDetector.Detect(repositoryRoot, baseCommit, headCommit);
        bool requiresTopology = false;
        int recoveryDeclarations = 0;
        HashSet<string> retainedLocators = new(StringComparer.Ordinal);
        HashSet<string> resultPaths = new(StringComparer.OrdinalIgnoreCase);

        foreach (TransitionRecord transition in transitions)
        {
            JsonObject contract = EvidenceJson.LoadContract(transition.ContractPath);
            _ = StoryEvidenceValidator.PreflightProductionContract(
                new GateOptions
                {
                    RepositoryRoot = repositoryRoot,
                    PolicyPath = policyPath,
                    StoryPath = Path.Combine(repositoryRoot, transition.StoryPath),
                    ContractPath = transition.ContractPath,
                    TargetStatus = "done",
                    BaseCommit = baseCommit,
                    HeadCommit = headCommit,
                    ResultsRoot = resultsRoot,
                },
                policy,
                contract);

            JsonArray results = EvidenceJson.RequiredArray(contract, "results", GateReason.MachineResultsInvalid);
            HashSet<string> lanes = new(StringComparer.Ordinal);
            foreach (JsonNode? node in results)
            {
                JsonObject lane = node as JsonObject
                    ?? throw new GateValidationException(GateReason.MachineResultsInvalid, "results");
                string laneName = EvidenceJson.RequiredString(lane, "lane", GateReason.MachineResultsInvalid);
                if (!lanes.Add(laneName))
                {
                    throw new GateValidationException(GateReason.MachineResultsInvalid, "duplicate-lane");
                }

                string trx = EvidenceJson.RequiredString(lane, "trx", GateReason.MachineResultsInvalid);
                string provenance = EvidenceJson.RequiredString(
                    lane,
                    "provenance",
                    GateReason.EvidenceStaleOrUnbound);
                string trxPath = TrxEvidenceReader.ResolveSafeResultPath(resultsRoot, trx);
                string provenancePath = TrxEvidenceReader.ResolveSafeResultPath(resultsRoot, provenance);
                if (!resultPaths.Add(trxPath) || !resultPaths.Add(provenancePath))
                {
                    throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "result-path-collision");
                }

                string source = EvidenceJson.RequiredString(lane, "source", GateReason.EvidenceStaleOrUnbound);
                if (source.Equals("retained", StringComparison.Ordinal))
                {
                    retainedLocators.Add(EvidenceJson.RequiredString(
                        lane,
                        "artifactLocator",
                        GateReason.EvidenceStaleOrUnbound));
                }
                else if (!source.Equals("current-run", StringComparison.Ordinal))
                {
                    throw new GateValidationException(GateReason.EvidenceStaleOrUnbound, "source");
                }

                requiresTopology |= source.Equals("current-run", StringComparison.Ordinal)
                    && laneName.Equals("aspire-dapr-primary", StringComparison.Ordinal);
                if (laneName.Equals(RecoveryLane, StringComparison.Ordinal))
                {
                    recoveryDeclarations++;
                    ValidateRecoveryLane(contract, lane, source, trx, provenance);
                }
            }
        }

        if (recoveryDeclarations > 1)
        {
            throw new GateValidationException(GateReason.PrimaryPathNotExecuted, "recovery-multiplicity");
        }

        return new CompletionProductionPlan(
            requiresTopology,
            recoveryDeclarations == 1,
            retainedLocators.Order(StringComparer.Ordinal).ToArray());
    }

    private static void ValidateRecoveryLane(
        JsonObject contract,
        JsonObject lane,
        string source,
        string trx,
        string provenance)
    {
        string locator = EvidenceJson.RequiredString(lane, "artifactLocator", GateReason.EvidenceStaleOrUnbound);
        IReadOnlyList<string> selectors = EvidenceJson.RequiredStrings(
            lane,
            "selectors",
            GateReason.PrimaryPathNotExecuted);
        string? primaryClass = EvidenceJson.RequiredNullableString(
            lane,
            "primaryPathClass",
            GateReason.PrimaryPathNotExecuted);
        bool declared = EvidenceJson.RequiredArray(contract, "primaryPaths", GateReason.PrimaryPathNotExecuted)
            .OfType<JsonObject>()
            .Count(declaration => EvidenceJson.RequiredString(
                    declaration,
                    "class",
                    GateReason.PrimaryPathNotExecuted).Equals(RecoveryClass, StringComparison.Ordinal)
                && EvidenceJson.RequiredString(
                    declaration,
                    "lane",
                    GateReason.PrimaryPathNotExecuted).Equals(RecoveryLane, StringComparison.Ordinal)) == 1;
        if (!source.Equals("current-run", StringComparison.Ordinal)
            || !trx.Equals(RecoveryTrx, StringComparison.Ordinal)
            || !provenance.Equals(RecoveryProvenance, StringComparison.Ordinal)
            || !locator.Equals($"file:{RecoveryTrx}", StringComparison.Ordinal)
            || !string.Equals(primaryClass, RecoveryClass, StringComparison.Ordinal)
            || !selectors.SequenceEqual([RecoverySelector], StringComparer.Ordinal)
            || EvidenceJson.RequiredBoolean(lane, "allowSkipped", GateReason.PrimaryPathNotExecuted)
            || !declared)
        {
            throw new GateValidationException(GateReason.PrimaryPathNotExecuted, RecoveryClass);
        }
    }
}
