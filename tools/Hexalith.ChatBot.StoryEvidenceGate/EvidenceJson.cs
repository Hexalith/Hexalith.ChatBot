using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Loads and validates the versioned policy, contract, and provenance JSON grammars.
/// </summary>
public static partial class EvidenceJson
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ContractProperties =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["root"] = Set(
                "schemaVersion", "recordKind", "recordLedgerKey", "storyKey", "storyTitle", "storyPath", "targetStatus",
                "persistedStatus", "sprintStatusKey", "bootstrap", "scope", "results", "primaryPaths",
                "mappings", "outOfScopeDisclosures", "reportPath"),
            ["scope"] = Set("mode", "implementationDigest", "repositories", "transitionPaths"),
            ["repositories"] = Set("name", "path", "baseCommit", "headCommit", "includeWorkingTree", "includePaths"),
            ["results"] = Set(
                "lane", "trx", "provenance", "artifactLocator", "source", "selectors", "allowSkipped",
                "primaryPathClass"),
            ["primaryPaths"] = Set("class", "lane"),
            ["mappings"] = Set("id", "kind", "paths", "assertions"),
            ["outOfScopeDisclosures"] = Set("repository", "path", "owner", "reason", "classification"),
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PolicyProperties =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["root"] = Set(
                "schemaVersion", "minimumSupportedVersion", "repositoryIdentity", "maximumCurrentRunAgeMinutes",
                "maximumRetainedEvidenceAgeHours", "maximumFutureClockSkewMinutes", "allowedScopeModes", "acceptedResultFormats",
                "requiredStorySections", "mandatoryCheckboxSections", "sourceDigest", "reasonCodes",
                "storyGrammars", "eventBaseHeadResolution", "primaryPathTriggers", "metadataOnly",
                "reportExcludedPaths", "exceptions"),
            ["sourceDigest"] = Set(
                "algorithm", "tuple", "sort", "rootDeclaredSubmodulesOnly", "immutableContentSource",
                "worktreeModeSource", "symlinkMode"),
            ["storyGrammars"] = Set(
                "name", "titleSource", "statusSource", "tasksSection", "acceptanceSection", "fileListSection"),
            ["eventBaseHeadResolution"] = Set(
                "schemaVersion", "pullRequestBase", "pullRequestHead", "pushBase", "pushHead",
                "zeroPushBaseFallback", "unavailableNonZeroPushBase", "nonPushEventRange"),
            ["primaryPathTriggers"] = Set(
                "class", "pathPatterns", "claimPatterns", "recognizedLanes", "recognizedLaneBindings"),
            ["recognizedLaneBindings"] = Set("lane", "selector", "trx", "provenance", "sources"),
            ["metadataOnly"] = Set(
                "forbiddenFieldNames", "allowedArtifactFields", "allowedLocatorSchemes", "maximumStringLength",
                "redactedFailureSubject"),
            ["exceptions"] = Set("id", "version", "expiresAtUtc", "reason", "allowedReasonCodes"),
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ProvenanceProperties =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["root"] = Set(
                "schemaVersion", "baseCommit", "headCommit", "implementationDigest", "trxSha256", "lane",
                "source", "selectors", "producedAtUtc", "artifactLocator", "repositoryIdentity"),
        };

    /// <summary>Loads a strict evidence contract.</summary>
    /// <param name="path">The JSON path.</param>
    /// <returns>The parsed object.</returns>
    public static JsonObject LoadContract(string path)
    {
        JsonObject contract = LoadObject(path, GateReason.ScopeDigestMismatch);
        ValidateContract(contract);
        return contract;
    }

    /// <summary>Parses a strict contract from an exact Git blob.</summary>
    /// <param name="json">The exact JSON text.</param>
    /// <param name="subject">The metadata-only source subject.</param>
    /// <returns>The parsed contract.</returns>
    public static JsonObject ParseContract(string json, string subject)
    {
        JsonObject contract;
        try
        {
            contract = JsonNode.Parse(
                json,
                documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                }) as JsonObject ?? throw new JsonException("Contract root is not an object.");
        }
        catch (JsonException)
        {
            throw new GateValidationException(GateReason.ScopeDigestMismatch, subject);
        }

        ValidateContract(contract);
        return contract;
    }

    private static void ValidateContract(JsonObject contract)
    {
        ValidateShape(contract, ContractProperties, "root");
        RejectForbiddenEvidenceFields(contract);
        RejectUnsafeMetadataValues(contract);
        _ = RequiredStoryKey(contract);
    }

    /// <summary>Gets a filename-safe story identity.</summary>
    public static string RequiredStoryKey(JsonObject contract)
    {
        string storyKey = RequiredString(contract, "storyKey", GateReason.StatusMismatch);
        if (storyKey.Length > 128
            || storyKey[0] == '-'
            || storyKey[^1] == '-'
            || storyKey.Contains("--", StringComparison.Ordinal)
            || storyKey.Any(static character => !(character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character == '-')))
        {
            throw new GateValidationException(GateReason.StatusMismatch, "story-key");
        }

        return storyKey;
    }

    /// <summary>Loads a strict story-evidence policy.</summary>
    /// <param name="path">The JSON path.</param>
    /// <returns>The parsed object.</returns>
    public static JsonObject LoadPolicy(string path)
    {
        JsonObject policy = LoadObject(path, GateReason.ScopeDigestMismatch);
        ValidateShape(policy, PolicyProperties, "root");
        return policy;
    }

    /// <summary>Loads a strict provenance sidecar.</summary>
    /// <param name="path">The JSON path.</param>
    /// <returns>The parsed object.</returns>
    public static JsonObject LoadProvenance(string path)
    {
        JsonObject provenance = LoadObject(path, GateReason.EvidenceStaleOrUnbound);
        ValidateShape(provenance, ProvenanceProperties, "root");
        RejectForbiddenEvidenceFields(provenance);
        RejectUnsafeMetadataValues(provenance);
        return provenance;
    }

    /// <summary>Gets a required string.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The non-empty string.</returns>
    public static string RequiredString(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value[name] is not JsonValue node
            || !node.TryGetValue(out string? result)
            || string.IsNullOrWhiteSpace(result))
        {
            throw new GateValidationException(reasonCode, name);
        }

        return result;
    }

    /// <summary>Gets a required property whose value is either null or a non-empty string.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The string, or null.</returns>
    public static string? RequiredNullableString(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.TryGetPropertyValue(name, out JsonNode? node))
        {
            throw new GateValidationException(reasonCode, name);
        }

        if (node is null)
        {
            return null;
        }

        if (node is not JsonValue jsonValue
            || !jsonValue.TryGetValue(out string? result)
            || string.IsNullOrWhiteSpace(result))
        {
            throw new GateValidationException(reasonCode, name);
        }

        return result;
    }

    /// <summary>Gets a required Boolean.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The Boolean value.</returns>
    public static bool RequiredBoolean(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value[name] is not JsonValue node || !node.TryGetValue(out bool result))
        {
            throw new GateValidationException(reasonCode, name);
        }

        return result;
    }

    /// <summary>Gets a required integer.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The integer value.</returns>
    public static int RequiredInteger(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value[name] is not JsonValue node || !node.TryGetValue(out int result))
        {
            throw new GateValidationException(reasonCode, name);
        }

        return result;
    }

    /// <summary>Gets a required object.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The object value.</returns>
    public static JsonObject RequiredObject(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value[name] as JsonObject ?? throw new GateValidationException(reasonCode, name);
    }

    /// <summary>Gets a required array.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The array value.</returns>
    public static JsonArray RequiredArray(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value[name] as JsonArray ?? throw new GateValidationException(reasonCode, name);
    }

    /// <summary>Gets all strings from a required array.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The string values.</returns>
    public static IReadOnlyList<string> RequiredStrings(JsonObject value, string name, string reasonCode)
    {
        JsonArray values = RequiredArray(value, name, reasonCode);
        List<string> result = [];
        foreach (JsonNode? item in values)
        {
            if (item is not JsonValue node
                || !node.TryGetValue(out string? text)
                || string.IsNullOrWhiteSpace(text))
            {
                throw new GateValidationException(reasonCode, name);
            }

            result.Add(text);
        }

        if (result.Distinct(StringComparer.Ordinal).Count() != result.Count)
        {
            throw new GateValidationException(reasonCode, name);
        }

        return result;
    }

    private static JsonObject LoadObject(string path, string reasonCode)
    {
        try
        {
            if (!File.Exists(path))
            {
                throw new GateValidationException(reasonCode, Path.GetFileName(path));
            }

            JsonNode? node = JsonNode.Parse(
                File.ReadAllText(path),
                documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                });
            return node as JsonObject ?? throw new GateValidationException(reasonCode, Path.GetFileName(path));
        }
        catch (Exception exception) when (exception is JsonException
            or IOException
            or ArgumentException
            or UnauthorizedAccessException
            or System.Security.SecurityException
            or NotSupportedException)
        {
            throw new GateValidationException(reasonCode, Path.GetFileName(path));
        }
    }

    private static void ValidateShape(
        JsonObject value,
        IReadOnlyDictionary<string, IReadOnlySet<string>> grammar,
        string context)
    {
        if (!grammar.TryGetValue(context, out IReadOnlySet<string>? allowed))
        {
            return;
        }

        foreach ((string propertyName, JsonNode? child) in value)
        {
            if (!allowed.Contains(propertyName))
            {
                throw new GateValidationException(GateReason.EvidencePayloadForbidden, propertyName);
            }

            if (child is JsonObject childObject && grammar.ContainsKey(propertyName))
            {
                ValidateShape(childObject, grammar, propertyName);
            }
            else if (child is JsonArray array && grammar.ContainsKey(propertyName))
            {
                foreach (JsonNode? item in array)
                {
                    if (item is not JsonObject itemObject)
                    {
                        throw new GateValidationException(GateReason.ScopeDigestMismatch, propertyName);
                    }

                    ValidateShape(itemObject, grammar, propertyName);
                }
            }
        }
    }

    private static void RejectForbiddenEvidenceFields(JsonNode node)
    {
        if (node is JsonObject value)
        {
            foreach ((string name, JsonNode? child) in value)
            {
                string normalized = name.ToLowerInvariant();
                if (normalized.Contains("secret", StringComparison.Ordinal)
                    || normalized.Contains("password", StringComparison.Ordinal)
                    || normalized.Contains("credential", StringComparison.Ordinal)
                    || normalized.Contains("payload", StringComparison.Ordinal)
                    || normalized.Contains("prompt", StringComparison.Ordinal)
                    || normalized.Contains("token", StringComparison.Ordinal))
                {
                    throw new GateValidationException(GateReason.EvidencePayloadForbidden, name);
                }

                if (child is not null)
                {
                    RejectForbiddenEvidenceFields(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array)
            {
                if (child is not null)
                {
                    RejectForbiddenEvidenceFields(child);
                }
            }
        }
    }

    private static void RejectUnsafeMetadataValues(JsonNode node)
    {
        if (node is JsonObject value)
        {
            foreach ((string name, JsonNode? child) in value)
            {
                if (child is JsonValue scalar && scalar.TryGetValue(out string? text))
                {
                    if (text.Length > 512
                        || text.Any(char.IsControl)
                        || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
                        || Regexes.UnsafeCredential().IsMatch(text))
                    {
                        throw new GateValidationException(GateReason.EvidencePayloadForbidden, name);
                    }
                }
                else if (child is not null)
                {
                    RejectUnsafeMetadataValues(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array)
            {
                if (child is JsonValue scalar && scalar.TryGetValue(out string? text))
                {
                    if (text.Length > 512
                        || text.Any(char.IsControl)
                        || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
                        || Regexes.UnsafeCredential().IsMatch(text))
                    {
                        throw new GateValidationException(GateReason.EvidencePayloadForbidden, "metadata-value");
                    }
                }
                else if (child is not null)
                {
                    RejectUnsafeMetadataValues(child);
                }
            }
        }
    }

    private static partial class Regexes
    {
        [System.Text.RegularExpressions.GeneratedRegex(
            "(?:secret|token|password|credential)(?:=|%3[dD])",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
        public static partial System.Text.RegularExpressions.Regex UnsafeCredential();
    }

    private static IReadOnlySet<string> Set(params string[] values) =>
        values.ToHashSet(StringComparer.Ordinal);
}
