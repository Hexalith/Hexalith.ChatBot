using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Detects explicit story or sprint-ledger completion transitions without numeric identity inference.
/// </summary>
public static class TransitionDetector
{
    /// <summary>Detects transitions between exact revisions.</summary>
    /// <param name="repositoryRoot">The root repository.</param>
    /// <param name="baseCommit">The exact base revision.</param>
    /// <param name="headCommit">The exact head revision.</param>
    /// <returns>The explicit transition records.</returns>
    public static IReadOnlyList<TransitionRecord> Detect(
        string repositoryRoot,
        string baseCommit,
        string headCommit)
    {
        _ = GitReader.ResolveExactCommit(repositoryRoot, baseCommit);
        _ = GitReader.ResolveExactCommit(repositoryRoot, headCommit);
        IReadOnlyDictionary<string, string> changed = GitReader.Diff(repositoryRoot, baseCommit, headCommit);
        List<JsonObject> contracts = LoadContracts(repositoryRoot);
        Dictionary<string, TransitionRecord> transitions = new(StringComparer.Ordinal);

        foreach (string path in changed.Keys.Where(static path =>
                     path.StartsWith("_bmad-output/implementation-artifacts/", StringComparison.Ordinal)
                     && path.EndsWith(".md", StringComparison.Ordinal)
                     && !path.Contains("/evidence/", StringComparison.Ordinal)))
        {
            string? before = GitReader.Show(repositoryRoot, baseCommit, path);
            string? after = GitReader.Show(repositoryRoot, headCommit, path);
            if (after is null)
            {
                continue;
            }

            string afterStatus = MarkdownStoryReader.ReadStatus(after);
            if (afterStatus is not ("done" or "complete"))
            {
                continue;
            }

            JsonObject[] matches = contracts.Where(value =>
                NormalizePath(EvidenceJson.RequiredString(value, "storyPath", GateReason.StatusMismatch))
                    .Equals(path, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new GateValidationException(GateReason.StatusMismatch, path);
            }

            JsonObject contract = matches[0];
            string terminalStatus = EvidenceJson.RequiredString(contract, "recordKind", GateReason.StatusMismatch)
                    .Equals("technicalEnabler", StringComparison.Ordinal)
                ? EvidenceJson.RequiredString(contract, "persistedStatus", GateReason.StatusMismatch)
                : "done";
            if (!afterStatus.Equals(terminalStatus, StringComparison.Ordinal)
                || (before is not null && MarkdownStoryReader.ReadStatus(before).Equals(terminalStatus, StringComparison.Ordinal)))
            {
                continue;
            }

            AddTransition(transitions, repositoryRoot, contract);
        }

        foreach (JsonObject contract in contracts.Where(value =>
                     EvidenceJson.RequiredBoolean(value, "bootstrap", GateReason.StatusMismatch)
                     && EvidenceJson.RequiredString(value, "recordKind", GateReason.StatusMismatch)
                         .Equals("technicalEnabler", StringComparison.Ordinal)))
        {
            string storyKey = EvidenceJson.RequiredString(contract, "storyKey", GateReason.StatusMismatch);
            string contractPath = $"_bmad-output/implementation-artifacts/evidence/{storyKey}.json";
            if (changed.ContainsKey(contractPath))
            {
                AddTransition(transitions, repositoryRoot, contract);
            }
        }

        const string TechnicalLedgerPath = "_bmad-output/planning-artifacts/technical-enablers.md";
        if (changed.ContainsKey(TechnicalLedgerPath))
        {
            string before = GitReader.Show(repositoryRoot, baseCommit, TechnicalLedgerPath) ?? string.Empty;
            string after = GitReader.Show(repositoryRoot, headCommit, TechnicalLedgerPath) ?? string.Empty;
            IReadOnlyDictionary<string, string> beforeStatuses = TechnicalEnablerLedgerReader.StatusesFromText(before);
            IReadOnlyDictionary<string, string> afterStatuses = TechnicalEnablerLedgerReader.StatusesFromText(after);
            foreach ((string key, string status) in afterStatuses)
            {
                if (!status.Equals("complete", StringComparison.Ordinal)
                    || (beforeStatuses.TryGetValue(key, out string? previous)
                        && previous.Equals("complete", StringComparison.Ordinal)))
                {
                    continue;
                }

                JsonObject contract = SingleContract(contracts, "recordLedgerKey", key, technicalEnablerOnly: true);
                AddTransition(transitions, repositoryRoot, contract);
            }
        }

        const string SprintPath = "_bmad-output/implementation-artifacts/sprint-status.yaml";
        if (changed.ContainsKey(SprintPath))
        {
            string before = GitReader.Show(repositoryRoot, baseCommit, SprintPath) ?? string.Empty;
            string after = GitReader.Show(repositoryRoot, headCommit, SprintPath) ?? string.Empty;
            IReadOnlyDictionary<string, string> beforeStatuses = ParseStatuses(before);
            IReadOnlyDictionary<string, string> afterStatuses = ParseStatuses(after);
            foreach ((string key, string status) in afterStatuses)
            {
                if (!status.Equals("done", StringComparison.Ordinal)
                    || (beforeStatuses.TryGetValue(key, out string? previous)
                        && previous.Equals("done", StringComparison.Ordinal)))
                {
                    continue;
                }

                JsonObject contract = contracts.SingleOrDefault(value =>
                    EvidenceJson.RequiredString(value, "sprintStatusKey", GateReason.StatusMismatch)
                        .Equals(key, StringComparison.Ordinal))
                    ?? throw new GateValidationException(GateReason.StatusMismatch, key);
                AddTransition(transitions, repositoryRoot, contract);
            }

            IReadOnlyDictionary<string, string> beforeActions = SprintLedgerReader.ActionStatusesFromText(before);
            IReadOnlyDictionary<string, string> afterActions = SprintLedgerReader.ActionStatusesFromText(after);
            foreach ((string action, string status) in afterActions)
            {
                if (!status.Equals("done", StringComparison.Ordinal)
                    || (beforeActions.TryGetValue(action, out string? previous)
                        && previous.Equals("done", StringComparison.Ordinal)))
                {
                    continue;
                }

                JsonObject contract = SingleContract(contracts, "sprintStatusKey", action, technicalEnablerOnly: true);
                AddTransition(transitions, repositoryRoot, contract);
            }
        }

        return transitions.Values.OrderBy(static value => value.StoryKey, StringComparer.Ordinal).ToArray();
    }

    private static JsonObject SingleContract(
        IEnumerable<JsonObject> contracts,
        string field,
        string value,
        bool technicalEnablerOnly)
    {
        JsonObject[] matches = contracts.Where(contract =>
                (!technicalEnablerOnly
                    || (EvidenceJson.RequiredString(contract, "recordKind", GateReason.StatusMismatch)
                            .Equals("technicalEnabler", StringComparison.Ordinal)
                        && !EvidenceJson.RequiredBoolean(contract, "bootstrap", GateReason.StatusMismatch)))
                && EvidenceJson.RequiredString(contract, field, GateReason.StatusMismatch)
                    .Equals(value, StringComparison.Ordinal))
            .ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new GateValidationException(GateReason.StatusMismatch, value);
    }

    private static List<JsonObject> LoadContracts(string repositoryRoot)
    {
        string evidenceRoot = Path.Combine(repositoryRoot, "_bmad-output", "implementation-artifacts", "evidence");
        if (!Directory.Exists(evidenceRoot))
        {
            return [];
        }

        List<JsonObject> contracts = [];
        HashSet<string> storyKeys = new(StringComparer.Ordinal);
        HashSet<string> storyPaths = new(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(evidenceRoot, "*.json", SearchOption.TopDirectoryOnly))
        {
            JsonObject contract = EvidenceJson.LoadContract(path);
            string storyKey = EvidenceJson.RequiredString(contract, "storyKey", GateReason.StatusMismatch);
            string storyPath = NormalizePath(EvidenceJson.RequiredString(contract, "storyPath", GateReason.StatusMismatch));
            if (!Path.GetFileNameWithoutExtension(path).Equals(storyKey, StringComparison.Ordinal)
                || !storyKeys.Add(storyKey)
                || !storyPaths.Add(storyPath))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "ambiguous-contract-identity");
            }

            contracts.Add(contract);
        }

        return contracts;
    }

    private static void AddTransition(
        IDictionary<string, TransitionRecord> transitions,
        string repositoryRoot,
        JsonObject contract)
    {
        string storyPath = NormalizePath(EvidenceJson.RequiredString(contract, "storyPath", GateReason.StatusMismatch));
        string storyKey = EvidenceJson.RequiredString(contract, "storyKey", GateReason.StatusMismatch);
        string contractPath = Path.Combine(
            repositoryRoot,
            "_bmad-output",
            "implementation-artifacts",
            "evidence",
            $"{storyKey}.json");
        if (!File.Exists(contractPath))
        {
            throw new GateValidationException(GateReason.StatusMismatch, storyKey);
        }

        TransitionRecord record = new(storyPath, contractPath, storyKey);
        if (transitions.TryGetValue(storyKey, out TransitionRecord? existing))
        {
            if (!existing.Equals(record))
            {
                throw new GateValidationException(GateReason.StatusMismatch, storyKey);
            }

            return;
        }

        transitions.Add(storyKey, record);
    }

    private static IReadOnlyDictionary<string, string> ParseStatuses(string yaml)
    {
        return SprintLedgerReader.StoryStatusesFromText(yaml);
    }

    private static string NormalizePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal)
            || (normalized.Length >= 3 && char.IsLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/')
            || normalized.Split('/').Any(static segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "path");
        }

        return normalized;
    }

}
