using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>Checks complete policy bindings before granting a primary lane's freshness override.</summary>
internal static class PrimaryLaneBinding
{
    /// <summary>Resolves a freshness ceiling only after checking every override binding for the result.</summary>
    internal static int ResolveCurrentRunAgeMinutes(JsonObject policy, JsonObject contract, JsonObject result)
    {
        string lane = EvidenceJson.RequiredString(result, "lane", GateReason.MachineResultsInvalid);
        int maximumAge = EvidenceJson.ResolveCurrentRunAgeMinutes(policy, lane);
        foreach (JsonObject trigger in EvidenceJson.RequiredArray(
                     policy, "primaryPathTriggers", GateReason.PrimaryPathNotExecuted).OfType<JsonObject>())
        {
            foreach (JsonObject binding in EvidenceJson.RequiredArray(
                         trigger, "recognizedLaneBindings", GateReason.PrimaryPathNotExecuted).OfType<JsonObject>())
            {
                if (EvidenceJson.RequiredString(binding, "lane", GateReason.PrimaryPathNotExecuted)
                        .Equals(lane, StringComparison.Ordinal)
                    && EvidenceJson.OptionalInteger(
                        binding, "maximumCurrentRunAgeMinutes", GateReason.EvidenceStaleOrUnbound) is not null)
                {
                    Validate(contract, trigger, binding, result);
                }
            }
        }

        return maximumAge;
    }

    /// <summary>Validates the declaration, class, selector, source, paths, and skip policy of one binding.</summary>
    internal static void Validate(JsonObject contract, JsonObject trigger, JsonObject binding, JsonObject result)
    {
        string pathClass = EvidenceJson.RequiredString(trigger, "class", GateReason.PrimaryPathNotExecuted);
        string lane = EvidenceJson.RequiredString(binding, "lane", GateReason.PrimaryPathNotExecuted);
        JsonObject[] declarations = EvidenceJson.RequiredArray(
                contract, "primaryPaths", GateReason.PrimaryPathNotExecuted)
            .OfType<JsonObject>()
            .Where(declaration => EvidenceJson.RequiredString(declaration, "class", GateReason.PrimaryPathNotExecuted)
                .Equals(pathClass, StringComparison.Ordinal))
            .ToArray();
        if (declarations.Length != 1
            || !EvidenceJson.RequiredString(declarations[0], "lane", GateReason.PrimaryPathNotExecuted)
                .Equals(lane, StringComparison.Ordinal)
            || !EvidenceJson.RequiredStrings(trigger, "recognizedLanes", GateReason.PrimaryPathNotExecuted)
                .Contains(lane, StringComparer.Ordinal)
            || !EvidenceJson.RequiredString(result, "lane", GateReason.PrimaryPathNotExecuted)
                .Equals(lane, StringComparison.Ordinal)
            || !string.Equals(
                EvidenceJson.RequiredNullableString(result, "primaryPathClass", GateReason.PrimaryPathNotExecuted),
                pathClass,
                StringComparison.Ordinal)
            || !EvidenceJson.RequiredStrings(result, "selectors", GateReason.PrimaryPathNotExecuted)
                .Contains(
                    EvidenceJson.RequiredString(binding, "selector", GateReason.PrimaryPathNotExecuted),
                    StringComparer.Ordinal)
            || !EvidenceJson.RequiredStrings(binding, "sources", GateReason.PrimaryPathNotExecuted)
                .Contains(
                    EvidenceJson.RequiredString(result, "source", GateReason.EvidenceStaleOrUnbound),
                    StringComparer.Ordinal)
            || EvidenceJson.RequiredBoolean(result, "allowSkipped", GateReason.PrimaryPathNotExecuted)
            || !MatchesPinnedPath(binding, result, "trx")
            || !MatchesPinnedPath(binding, result, "provenance"))
        {
            throw new GateValidationException(GateReason.PrimaryPathNotExecuted, pathClass);
        }
    }

    private static bool MatchesPinnedPath(JsonObject binding, JsonObject result, string property) =>
        binding[property] is null
        || EvidenceJson.RequiredString(binding, property, GateReason.PrimaryPathNotExecuted)
            .Equals(
                EvidenceJson.RequiredString(result, property, GateReason.PrimaryPathNotExecuted),
                StringComparison.Ordinal);
}
