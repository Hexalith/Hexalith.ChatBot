namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Reads an explicitly identified technical-enabler record from the planning ledger.
/// </summary>
public static class TechnicalEnablerLedgerReader
{
    /// <summary>Reads the leading status word for an exact technical-enabler heading.</summary>
    /// <param name="path">The technical-enabler ledger path.</param>
    /// <param name="recordKey">The exact record key, such as TE-2.</param>
    /// <returns>The normalized status word, or null when absent.</returns>
    public static string? Status(string path, string recordKey)
    {
        return StatusFromText(File.ReadAllText(path), recordKey);
    }

    /// <summary>Reads the leading status word from exact ledger text.</summary>
    /// <param name="text">The ledger text.</param>
    /// <param name="recordKey">The exact record key.</param>
    /// <returns>The normalized status word, or null when absent.</returns>
    public static string? StatusFromText(string text, string recordKey)
    {
        ArgumentNullException.ThrowIfNull(text);
        return StatusesFromText(text).TryGetValue(recordKey, out string? status) ? status : null;
    }

    /// <summary>Reads every explicitly headed technical-enabler status.</summary>
    /// <param name="text">The ledger text.</param>
    /// <returns>Exact record keys and statuses.</returns>
    public static IReadOnlyDictionary<string, string> StatusesFromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        HashSet<string> headings = new(StringComparer.Ordinal);
        string[] lines = MarkdownStoryReader.VisibleLines(text);
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            if (!line.StartsWith("## TE-", StringComparison.Ordinal))
            {
                continue;
            }

            int separator = line.IndexOf(" — ", StringComparison.Ordinal);
            if (separator <= 3)
            {
                continue;
            }

            string key = line[3..separator];
            if (!headings.Add(key))
            {
                throw new GateValidationException(GateReason.StatusMismatch, "duplicate-technical-enabler-heading");
            }

            string? status = null;
            for (int following = index + 1; following < lines.Length && !lines[following].StartsWith("## ", StringComparison.Ordinal); following++)
            {
                const string Prefix = "- **Status:** ";
                if (lines[following].StartsWith(Prefix, StringComparison.Ordinal))
                {
                    if (status is not null)
                    {
                        throw new GateValidationException(GateReason.StatusMismatch, "duplicate-technical-enabler-status");
                    }

                    string value = lines[following][Prefix.Length..];
                    int statusSeparator = value.IndexOfAny([';', ' ', '.']);
                    status = (statusSeparator < 0 ? value : value[..statusSeparator]).Trim().ToLowerInvariant();
                }
            }

            if (status is not null)
            {
                if (!result.TryAdd(key, status))
                {
                    throw new GateValidationException(GateReason.StatusMismatch, "duplicate-technical-enabler-heading");
                }
            }
        }

        return result;
    }
}
