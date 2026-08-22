using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Validates the fixed TE-2 lifecycle delta independently from the immutable HEAD snapshot.
/// </summary>
public static class LifecycleTransitionValidator
{
    private const string TechnicalLedgerPath = "_bmad-output/planning-artifacts/technical-enablers.md";
    private const string SprintLedgerPath = "_bmad-output/implementation-artifacts/sprint-status.yaml";

    /// <summary>Validates the exact four-path technical-enabler completion event.</summary>
    /// <param name="repositoryRoot">The root repository.</param>
    /// <param name="contract">The active HEAD contract.</param>
    /// <param name="baseCommit">The exact event base.</param>
    /// <param name="headCommit">The exact event head.</param>
    /// <param name="eventPaths">The independently computed base-to-head event paths.</param>
    public static void Validate(
        string repositoryRoot,
        JsonObject contract,
        string baseCommit,
        string headCommit,
        IReadOnlyList<ChangedPath> eventPaths)
    {
        JsonObject scope = EvidenceJson.RequiredObject(contract, "scope", GateReason.ScopeDigestMismatch);
        if (!EvidenceJson.RequiredString(scope, "mode", GateReason.ScopeDigestMismatch)
                .Equals("snapshot-plus-transition", StringComparison.Ordinal))
        {
            return;
        }

        string storyPath = NormalizePath(EvidenceJson.RequiredString(contract, "storyPath", GateReason.StatusMismatch));
        string storyKey = EvidenceJson.RequiredStoryKey(contract);
        string contractPath = $"_bmad-output/implementation-artifacts/evidence/{storyKey}.json";
        HashSet<string> expectedPaths =
        [
            storyPath,
            contractPath,
            TechnicalLedgerPath,
            SprintLedgerPath,
        ];
        HashSet<string> declaredPaths = EvidenceJson
            .RequiredStrings(scope, "transitionPaths", GateReason.StatusMismatch)
            .Select(NormalizePath)
            .ToHashSet(StringComparer.Ordinal);
        if (!declaredPaths.SetEquals(expectedPaths))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "transition-paths");
        }

        if (EvidenceJson.RequiredBoolean(contract, "bootstrap", GateReason.StatusMismatch))
        {
            return;
        }

        if (!EvidenceJson.RequiredString(contract, "recordKind", GateReason.StatusMismatch)
                .Equals("technicalEnabler", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "snapshot-record-kind");
        }

        JsonObject rootRepository = EvidenceJson.RequiredArray(scope, "repositories", GateReason.ScopeDigestMismatch)
            .Select(static node => node as JsonObject)
            .Where(static node => node is not null)
            .Single(node => EvidenceJson.RequiredString(node!, "path", GateReason.ScopeDigestMismatch)
                .Equals(".", StringComparison.Ordinal))!;
        string rootName = EvidenceJson.RequiredString(rootRepository, "name", GateReason.ScopeDigestMismatch);
        HashSet<string> actualPaths = eventPaths
            .Where(path => path.Repository.Equals(rootName, StringComparison.Ordinal))
            .Select(static path => path.Path)
            .ToHashSet(StringComparer.Ordinal);
        if (eventPaths.Any(path => !path.Repository.Equals(rootName, StringComparison.Ordinal))
            || !actualPaths.SetEquals(expectedPaths))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "lifecycle-event-paths");
        }

        byte[] baseStory = ReadBlob(repositoryRoot, baseCommit, storyPath);
        byte[] headStory = ReadBlob(repositoryRoot, headCommit, storyPath);
        AssertExactReplacement(baseStory, headStory, "status: 'in-review'"u8, "status: 'complete'"u8);

        byte[] baseContract = ReadBlob(repositoryRoot, baseCommit, contractPath);
        byte[] headContract = ReadBlob(repositoryRoot, headCommit, contractPath);
        AssertContractDelta(baseContract, headContract, contract);

        string recordKey = EvidenceJson.RequiredString(contract, "recordLedgerKey", GateReason.StatusMismatch);
        AssertLedgerDelta(
            ReadBlob(repositoryRoot, baseCommit, TechnicalLedgerPath),
            ReadBlob(repositoryRoot, headCommit, TechnicalLedgerPath),
            $"## {recordKey} ",
            "- **Status:** review",
            "- **Status:** complete");

        string action = EvidenceJson.RequiredString(contract, "sprintStatusKey", GateReason.StatusMismatch);
        AssertLedgerDelta(
            ReadBlob(repositoryRoot, baseCommit, SprintLedgerPath),
            ReadBlob(repositoryRoot, headCommit, SprintLedgerPath),
            $"    action: \"{action}\"",
            "    status: open",
            "    status: done",
            "  - epic:");
    }

    private static void AssertContractDelta(byte[] baseBytes, byte[] headBytes, JsonObject headContract)
    {
        JsonObject baseContract;
        try
        {
            baseContract = JsonNode.Parse(baseBytes)?.AsObject()
                ?? throw new JsonException("Contract is not an object.");
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "base-contract");
        }

        if (!EvidenceJson.RequiredBoolean(baseContract, "bootstrap", GateReason.StatusMismatch)
            || EvidenceJson.RequiredBoolean(headContract, "bootstrap", GateReason.StatusMismatch))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "bootstrap-transition");
        }

        _ = EvidenceJson.RequiredString(
            EvidenceJson.RequiredObject(baseContract, "scope", GateReason.StatusMismatch),
            "implementationDigest",
            GateReason.StatusMismatch);
        string headDigest = EvidenceJson.RequiredString(
            EvidenceJson.RequiredObject(headContract, "scope", GateReason.StatusMismatch),
            "implementationDigest",
            GateReason.StatusMismatch);
        byte[] expected = ReplaceJsonValues(baseBytes, headDigest);
        if (!expected.AsSpan().SequenceEqual(headBytes))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "contract-transition");
        }
    }

    private static byte[] ReplaceJsonValues(byte[] bytes, string headDigest)
    {
        Utf8JsonReader reader = new(bytes);
        int scopeDepth = -1;
        bool nextObjectIsScope = false;
        string? pending = null;
        List<(int Start, int End, byte[] Value)> replacements = [];
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string name = reader.GetString() ?? string.Empty;
                nextObjectIsScope = scopeDepth < 0 && reader.CurrentDepth == 1 && name.Equals("scope", StringComparison.Ordinal);
                pending = reader.CurrentDepth == 1 && name.Equals("bootstrap", StringComparison.Ordinal)
                    ? "bootstrap"
                    : scopeDepth >= 0 && reader.CurrentDepth == scopeDepth + 1
                        && name.Equals("implementationDigest", StringComparison.Ordinal)
                            ? "implementationDigest"
                            : null;
                continue;
            }

            if (nextObjectIsScope)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    scopeDepth = reader.CurrentDepth;
                }

                nextObjectIsScope = false;
            }

            if (pending is not null)
            {
                byte[] value = pending.Equals("bootstrap", StringComparison.Ordinal)
                    ? "false"u8.ToArray()
                    : JsonSerializer.SerializeToUtf8Bytes(headDigest);
                replacements.Add((checked((int)reader.TokenStartIndex), checked((int)reader.BytesConsumed), value));
                pending = null;
            }

            if (scopeDepth >= 0 && reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == scopeDepth)
            {
                scopeDepth = -1;
            }
        }

        if (replacements.Count != 2)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "contract-transition-fields");
        }

        using MemoryStream output = new();
        int offset = 0;
        foreach ((int start, int end, byte[] value) in replacements.OrderBy(static item => item.Start))
        {
            output.Write(bytes, offset, start - offset);
            output.Write(value);
            offset = end;
        }

        output.Write(bytes, offset, bytes.Length - offset);
        return output.ToArray();
    }

    private static void AssertLedgerDelta(
        byte[] baseBytes,
        byte[] headBytes,
        string recordMarker,
        string before,
        string after,
        string nextRecordMarker = "\n## ")
    {
        string baseText = Encoding.UTF8.GetString(baseBytes);
        int recordStart = baseText.IndexOf(recordMarker, StringComparison.Ordinal);
        if (recordStart < 0 || baseText.IndexOf(recordMarker, recordStart + recordMarker.Length, StringComparison.Ordinal) >= 0)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "lifecycle-record");
        }

        int recordEnd = baseText.IndexOf(nextRecordMarker, recordStart + recordMarker.Length, StringComparison.Ordinal);
        recordEnd = recordEnd < 0 ? baseText.Length : recordEnd;
        string block = baseText[recordStart..recordEnd];
        int valueOffset = block.IndexOf(before, StringComparison.Ordinal);
        if (valueOffset < 0 || block.IndexOf(before, valueOffset + before.Length, StringComparison.Ordinal) >= 0)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "lifecycle-value");
        }

        int absoluteOffset = recordStart + valueOffset;
        string expected = string.Concat(baseText.AsSpan(0, absoluteOffset), after, baseText.AsSpan(absoluteOffset + before.Length));
        if (!Encoding.UTF8.GetBytes(expected).AsSpan().SequenceEqual(headBytes))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "lifecycle-record-delta");
        }
    }

    private static void AssertExactReplacement(byte[] baseBytes, byte[] headBytes, ReadOnlySpan<byte> before, ReadOnlySpan<byte> after)
    {
        int offset = baseBytes.AsSpan().IndexOf(before);
        if (offset < 0 || baseBytes.AsSpan(offset + before.Length).IndexOf(before) >= 0)
        {
            throw new GateValidationException(GateReason.StatusMismatch, "story-status-transition");
        }

        using MemoryStream expected = new();
        expected.Write(baseBytes, 0, offset);
        expected.Write(after);
        expected.Write(baseBytes, offset + before.Length, baseBytes.Length - offset - before.Length);
        if (!expected.ToArray().AsSpan().SequenceEqual(headBytes))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "story-transition-delta");
        }
    }

    private static byte[] ReadBlob(string repositoryRoot, string revision, string path)
    {
        (string Mode, string ObjectId)? entry = GitReader.TreeEntry(repositoryRoot, revision, path);
        if (entry is null || !entry.Value.Mode.Equals("100644", StringComparison.Ordinal))
        {
            throw new GateValidationException(GateReason.StatusMismatch, path);
        }

        return GitReader.BlobBytes(repositoryRoot, entry.Value.ObjectId);
    }

    private static string NormalizePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.Split('/').Any(static segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "path");
        }

        return normalized;
    }
}
