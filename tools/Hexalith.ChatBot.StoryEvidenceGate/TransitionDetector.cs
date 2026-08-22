using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Detects explicit completion candidates before loading any unrelated evidence contract.
/// </summary>
public static class TransitionDetector
{
    private const string EvidencePrefix = "_bmad-output/implementation-artifacts/evidence/";
    private const string TechnicalLedgerPath = "_bmad-output/planning-artifacts/technical-enablers.md";
    private const string SprintPath = "_bmad-output/implementation-artifacts/sprint-status.yaml";

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
        Dictionary<string, TransitionRecord> transitions = new(StringComparer.Ordinal);
        HashSet<string> terminalContractCandidates = new(StringComparer.Ordinal);
        HashSet<string> independentlyCompletedCandidates = new(StringComparer.Ordinal);

        foreach (string path in changed.Keys.Where(IsContractPath))
        {
            string after = GitReader.Show(repositoryRoot, headCommit, path)
                ?? throw new GateValidationException(GateReason.StatusMismatch, path);
            JsonObject contract = EvidenceJson.ParseContract(after, Path.GetFileName(path));
            bool headBootstrap = EvidenceJson.RequiredBoolean(contract, "bootstrap", GateReason.StatusMismatch);
            string? before = GitReader.Show(repositoryRoot, baseCommit, path);
            if (headBootstrap)
            {
                if (before is not null && !ReadBootstrap(before, path))
                {
                    throw new GateValidationException(GateReason.StatusMismatch, "bootstrap-regression");
                }

                AddTransition(transitions, repositoryRoot, contract);
                continue;
            }

            if (before is null)
            {
                string storyKey = EvidenceJson.RequiredStoryKey(contract);
                terminalContractCandidates.Add(storyKey);
                AddTransition(transitions, repositoryRoot, contract);
                continue;
            }

            if (!EvidenceJson.RequiredBoolean(
                    EvidenceJson.ParseContract(before, Path.GetFileName(path)),
                    "bootstrap",
                    GateReason.StatusMismatch))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "completed-contract-change");
            }

            AddTransition(transitions, repositoryRoot, contract);
        }

        foreach (string path in changed.Keys.Where(IsStoryPath))
        {
            string? before = GitReader.Show(repositoryRoot, baseCommit, path);
            string? after = GitReader.Show(repositoryRoot, headCommit, path);
            if (after is null)
            {
                if (before is not null && MarkdownStoryReader.ReadStatus(before) is "done" or "complete")
                {
                    throw new GateValidationException(GateReason.StatusMismatch, "story-status-regression");
                }

                continue;
            }

            string afterStatus = MarkdownStoryReader.ReadStatus(after);
            string? beforeStatus = before is null ? null : MarkdownStoryReader.ReadStatus(before);
            if (beforeStatus is "done" or "complete" && afterStatus is not ("done" or "complete"))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "story-status-regression");
            }

            if (afterStatus is not ("done" or "complete")
                || beforeStatus?.Equals(afterStatus, StringComparison.Ordinal) == true)
            {
                continue;
            }

            JsonObject contract = FindSingleContract(
                repositoryRoot,
                candidate => CandidateField(candidate, "storyPath").Equals(path, StringComparison.Ordinal),
                path);
            AddTransition(transitions, repositoryRoot, contract);
            independentlyCompletedCandidates.Add(EvidenceJson.RequiredStoryKey(contract));
        }

        if (changed.ContainsKey(TechnicalLedgerPath))
        {
            string before = GitReader.Show(repositoryRoot, baseCommit, TechnicalLedgerPath) ?? string.Empty;
            string after = GitReader.Show(repositoryRoot, headCommit, TechnicalLedgerPath) ?? string.Empty;
            IReadOnlyDictionary<string, string> beforeStatuses = TechnicalEnablerLedgerReader.StatusesFromText(before);
            IReadOnlyDictionary<string, string> afterStatuses = TechnicalEnablerLedgerReader.StatusesFromText(after);
            RejectTerminalRegressions(beforeStatuses, afterStatuses, "complete", "technical-ledger-regression");
            foreach ((string key, string status) in afterStatuses)
            {
                if (!status.Equals("complete", StringComparison.Ordinal)
                    || (beforeStatuses.TryGetValue(key, out string? previous)
                        && previous.Equals("complete", StringComparison.Ordinal)))
                {
                    continue;
                }

                JsonObject contract = FindSingleContract(
                    repositoryRoot,
                    candidate => CandidateField(candidate, "recordKind").Equals("technicalEnabler", StringComparison.Ordinal)
                        && CandidateField(candidate, "recordLedgerKey").Equals(key, StringComparison.Ordinal),
                    key);
                AddTransition(transitions, repositoryRoot, contract);
                independentlyCompletedCandidates.Add(EvidenceJson.RequiredStoryKey(contract));
            }
        }

        if (changed.ContainsKey(SprintPath))
        {
            string before = GitReader.Show(repositoryRoot, baseCommit, SprintPath) ?? string.Empty;
            string after = GitReader.Show(repositoryRoot, headCommit, SprintPath) ?? string.Empty;
            AddSprintStoryCandidates(repositoryRoot, transitions, independentlyCompletedCandidates, before, after);
            AddSprintActionCandidates(repositoryRoot, transitions, independentlyCompletedCandidates, before, after);
        }

        terminalContractCandidates.ExceptWith(independentlyCompletedCandidates);
        if (terminalContractCandidates.Count != 0)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "completed-contract-change");
        }

        return transitions.Values.OrderBy(static value => value.StoryKey, StringComparer.Ordinal).ToArray();
    }

    private static void AddSprintStoryCandidates(
        string repositoryRoot,
        IDictionary<string, TransitionRecord> transitions,
        ISet<string> independentlyCompletedCandidates,
        string before,
        string after)
    {
        IReadOnlyDictionary<string, string> beforeStatuses = SprintLedgerReader.StoryStatusesFromText(before);
        IReadOnlyDictionary<string, string> afterStatuses = SprintLedgerReader.StoryStatusesFromText(after);
        RejectTerminalRegressions(beforeStatuses, afterStatuses, "done", "sprint-story-regression");
        foreach ((string key, string status) in afterStatuses)
        {
            if (!status.Equals("done", StringComparison.Ordinal)
                || (beforeStatuses.TryGetValue(key, out string? previous) && previous.Equals("done", StringComparison.Ordinal)))
            {
                continue;
            }

            JsonObject contract = FindSingleContract(
                repositoryRoot,
                candidate => CandidateField(candidate, "sprintStatusKey").Equals(key, StringComparison.Ordinal),
                key);
            AddTransition(transitions, repositoryRoot, contract);
            independentlyCompletedCandidates.Add(EvidenceJson.RequiredStoryKey(contract));
        }
    }

    private static void AddSprintActionCandidates(
        string repositoryRoot,
        IDictionary<string, TransitionRecord> transitions,
        ISet<string> independentlyCompletedCandidates,
        string before,
        string after)
    {
        IReadOnlyDictionary<string, string> beforeStatuses = SprintLedgerReader.ActionStatusesFromText(before);
        IReadOnlyDictionary<string, string> afterStatuses = SprintLedgerReader.ActionStatusesFromText(after);
        RejectTerminalRegressions(beforeStatuses, afterStatuses, "done", "sprint-action-regression");
        foreach ((string action, string status) in afterStatuses)
        {
            if (!status.Equals("done", StringComparison.Ordinal)
                || (beforeStatuses.TryGetValue(action, out string? previous) && previous.Equals("done", StringComparison.Ordinal)))
            {
                continue;
            }

            JsonObject contract = FindSingleContract(
                repositoryRoot,
                candidate => CandidateField(candidate, "recordKind").Equals("technicalEnabler", StringComparison.Ordinal)
                    && CandidateField(candidate, "sprintStatusKey").Equals(action, StringComparison.Ordinal),
                action);
            AddTransition(transitions, repositoryRoot, contract);
            independentlyCompletedCandidates.Add(EvidenceJson.RequiredStoryKey(contract));
        }
    }

    private static JsonObject FindSingleContract(
        string repositoryRoot,
        Func<JsonObject, bool> predicate,
        string subject)
    {
        string evidenceRoot = Path.Combine(repositoryRoot, "_bmad-output", "implementation-artifacts", "evidence");
        if (!Directory.Exists(evidenceRoot))
        {
            throw new GateValidationException(GateReason.StatusMismatch, subject);
        }

        List<(string Path, JsonObject Identity)> candidates = [];
        foreach (string path in Directory.EnumerateFiles(evidenceRoot, "*.json", SearchOption.TopDirectoryOnly))
        {
            JsonObject? candidate = TryParseIdentity(path);
            if (candidate is not null)
            {
                candidates.Add((path, candidate));
            }
        }

        List<(string Path, JsonObject Identity)> matches = candidates.Where(candidate => predicate(candidate.Identity)).ToList();

        if (matches.Count != 0)
        {
            HashSet<string> activeKeys = matches
                .Select(match => CandidateField(match.Identity, "storyKey"))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> activePaths = matches
                .Select(match => CandidateField(match.Identity, "storyPath"))
                .ToHashSet(StringComparer.Ordinal);
            if (candidates.GroupBy(candidate => CandidateField(candidate.Identity, "storyKey"), StringComparer.Ordinal)
                    .Any(group => activeKeys.Contains(group.Key) && group.Count() > 1)
                || candidates.GroupBy(candidate => CandidateField(candidate.Identity, "storyPath"), StringComparer.Ordinal)
                    .Any(group => activePaths.Contains(group.Key) && group.Count() > 1))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "duplicate-contract-identity");
            }
        }

        if (matches.Count != 1)
        {
            throw new GateValidationException(GateReason.StatusMismatch, subject);
        }

        (string selectedPath, JsonObject selectedIdentity) = matches[0];
        return EvidenceJson.LoadContract(selectedPath);
    }

    private static JsonObject? TryParseIdentity(string path)
    {
        try
        {
            JsonObject? candidate = JsonNode.Parse(
                File.ReadAllText(path),
                documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                }) as JsonObject;
            return candidate is not null
                && candidate["storyKey"] is JsonValue
                && candidate["storyPath"] is JsonValue
                ? candidate
                : null;
        }
        catch (Exception exception) when (exception is JsonException
            or IOException
            or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string CandidateField(JsonObject candidate, string name)
    {
        return candidate[name] is JsonValue value && value.TryGetValue(out string? text)
            ? text.Replace('\\', '/')
            : string.Empty;
    }

    private static void AddTransition(
        IDictionary<string, TransitionRecord> transitions,
        string repositoryRoot,
        JsonObject contract)
    {
        string storyPath = NormalizePath(EvidenceJson.RequiredString(contract, "storyPath", GateReason.StatusMismatch));
        string storyKey = EvidenceJson.RequiredStoryKey(contract);
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

    private static bool IsContractPath(string path)
    {
        string suffix = path.StartsWith(EvidencePrefix, StringComparison.Ordinal)
            ? path[EvidencePrefix.Length..]
            : string.Empty;
        return suffix.Length > 5
            && !suffix.Contains('/', StringComparison.Ordinal)
            && suffix.EndsWith(".json", StringComparison.Ordinal);
    }

    private static bool IsStoryPath(string path) =>
        path.StartsWith("_bmad-output/implementation-artifacts/", StringComparison.Ordinal)
        && path.EndsWith(".md", StringComparison.Ordinal)
        && !path.Contains("/evidence/", StringComparison.Ordinal);

    private static bool ReadBootstrap(string json, string subject)
    {
        try
        {
            JsonObject value = JsonNode.Parse(json)?.AsObject()
                ?? throw new JsonException("Contract root is not an object.");
            return EvidenceJson.RequiredBoolean(value, "bootstrap", GateReason.StatusMismatch);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new GateValidationException(GateReason.StatusMismatch, subject);
        }
    }

    private static void RejectTerminalRegressions(
        IReadOnlyDictionary<string, string> before,
        IReadOnlyDictionary<string, string> after,
        string terminal,
        string subject)
    {
        if (before.Any(pair => pair.Value.Equals(terminal, StringComparison.Ordinal)
                && (!after.TryGetValue(pair.Key, out string? current)
                    || !current.Equals(terminal, StringComparison.Ordinal))))
        {
            throw new GateValidationException(GateReason.StatusMismatch, subject);
        }
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
