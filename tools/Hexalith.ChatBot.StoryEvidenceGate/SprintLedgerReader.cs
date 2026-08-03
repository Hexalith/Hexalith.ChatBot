using System.Text.RegularExpressions;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Reads exact sprint-ledger keys and technical-enabler action records.
/// </summary>
public static partial class SprintLedgerReader
{
    /// <summary>Reads the exact development status for a story key.</summary>
    /// <param name="path">The sprint-status path.</param>
    /// <param name="key">The exact story key.</param>
    /// <returns>The status, or null when absent.</returns>
    public static string? StoryStatus(string path, string key)
    {
        return StoryStatusFromText(File.ReadAllText(path), key);
    }

    /// <summary>Reads an exact development status from sprint-ledger text.</summary>
    /// <param name="text">The sprint-ledger text.</param>
    /// <param name="key">The exact story key.</param>
    /// <returns>The status, or null when absent.</returns>
    public static string? StoryStatusFromText(string text, string key)
    {
        ArgumentNullException.ThrowIfNull(text);
        return StoryStatusesFromText(text).TryGetValue(key, out string? status) ? status : null;
    }

    /// <summary>Reads all exact development status records and rejects duplicate keys.</summary>
    public static IReadOnlyDictionary<string, string> StoryStatusesFromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        foreach (string line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            Match match = StoryEntry().Match(line);
            if (match.Success && !result.TryAdd(match.Groups[1].Value, match.Groups[2].Value))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "duplicate-sprint-key");
            }
        }

        return result;
    }

    /// <summary>Reads the status of an action whose text exactly matches the explicit key.</summary>
    /// <param name="path">The sprint-status path.</param>
    /// <param name="actionText">The exact action text.</param>
    /// <returns>The status, or null when absent.</returns>
    public static string? ActionStatus(string path, string actionText)
    {
        return ActionStatusFromText(File.ReadAllText(path), actionText);
    }

    /// <summary>Reads an exact action status from sprint-ledger text.</summary>
    /// <param name="text">The sprint-ledger text.</param>
    /// <param name="actionText">The exact action text.</param>
    /// <returns>The status, or null when absent.</returns>
    public static string? ActionStatusFromText(string text, string actionText)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ActionStatusesFromText(text).TryGetValue(actionText, out string? status) ? status : null;
    }

    /// <summary>Reads all exact technical-enabler action statuses from sprint-ledger text.</summary>
    /// <param name="text">The sprint-ledger text.</param>
    /// <returns>Action text and status pairs.</returns>
    public static IReadOnlyDictionary<string, string> ActionStatusesFromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        HashSet<string> actions = new(StringComparer.Ordinal);
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            Match action = ActionEntry().Match(lines[index]);
            if (!action.Success)
            {
                continue;
            }

            string actionText = action.Groups[1].Value;
            if (!actions.Add(actionText))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "duplicate-action");
            }

            string? status = null;
            for (int following = index + 1; following < lines.Length && !lines[following].StartsWith("  - epic:", StringComparison.Ordinal); following++)
            {
                Match statusMatch = StatusEntry().Match(lines[following]);
                if (statusMatch.Success)
                {
                    if (status is not null)
                    {
                        throw new GateValidationException(GateReason.StatusMismatch, "duplicate-action-status");
                    }

                    status = statusMatch.Groups[1].Value;
                }
            }

            if (status is not null && !result.TryAdd(actionText, status))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "duplicate-action");
            }
        }

        return result;
    }

    [GeneratedRegex("^  ([A-Za-z0-9][A-Za-z0-9_-]*): (\\S+)$", RegexOptions.CultureInvariant)]
    private static partial Regex StoryEntry();

    [GeneratedRegex("^    action: \"(.*)\"$", RegexOptions.CultureInvariant)]
    private static partial Regex ActionEntry();

    [GeneratedRegex("^    status: (\\S+)$", RegexOptions.CultureInvariant)]
    private static partial Regex StatusEntry();
}
