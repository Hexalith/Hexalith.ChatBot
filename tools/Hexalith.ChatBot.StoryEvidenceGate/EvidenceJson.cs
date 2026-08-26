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
                "maximumLaneCurrentRunAgeMinutes", "maximumRetainedEvidenceAgeHours", "maximumFutureClockSkewMinutes",
                "allowedScopeModes", "acceptedResultFormats",
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
            ["recognizedLaneBindings"] = Set(
                "lane", "selector", "trx", "provenance", "sources", "maximumCurrentRunAgeMinutes"),
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

    /// <summary>Gets an optional integer, or null when the property is absent or JSON null.</summary>
    /// <param name="value">The parent object.</param>
    /// <param name="name">The property name.</param>
    /// <param name="reasonCode">The failure reason.</param>
    /// <returns>The integer value, or null.</returns>
    public static int? OptionalInteger(JsonObject value, string name, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.TryGetPropertyValue(name, out JsonNode? node) || node is null)
        {
            return null;
        }

        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue(out int result))
        {
            throw new GateValidationException(reasonCode, name);
        }

        return result;
    }

    /// <summary>
    /// Resolves the effective current-run freshness ceiling for one lane.
    /// </summary>
    /// <remarks>
    /// The global <c>maximumCurrentRunAgeMinutes</c> is measured from the TRX finish time. A binding may raise its
    /// own ceiling when an earlier producer must remain valid while a later multi-hour recovery drill runs, but
    /// never lower it below the global floor or exceed <c>maximumLaneCurrentRunAgeMinutes</c>. All bindings for the
    /// lane are consulted, and disagreeing or present-versus-absent overrides fail closed rather than letting the
    /// first one win.
    /// </remarks>
    /// <param name="policy">The pinned policy.</param>
    /// <param name="laneName">The lane being evaluated.</param>
    /// <returns>The effective ceiling in minutes.</returns>
    public static int ResolveCurrentRunAgeMinutes(JsonObject policy, string laneName)
    {
        ArgumentNullException.ThrowIfNull(policy);
        int globalCeiling = RequiredInteger(
            policy,
            "maximumCurrentRunAgeMinutes",
            GateReason.EvidenceStaleOrUnbound);
        int maximumLaneCeiling = RequiredInteger(
            policy,
            "maximumLaneCurrentRunAgeMinutes",
            GateReason.EvidenceStaleOrUnbound);
        if (policy["primaryPathTriggers"] is not JsonArray triggers)
        {
            return globalCeiling;
        }

        int? resolved = null;
        bool? overrideDeclared = null;
        foreach (JsonNode? triggerNode in triggers)
        {
            if (triggerNode is not JsonObject trigger
                || trigger["recognizedLaneBindings"] is not JsonArray bindings)
            {
                continue;
            }

            foreach (JsonNode? bindingNode in bindings)
            {
                if (bindingNode is not JsonObject binding
                    || !RequiredString(binding, "lane", GateReason.EvidenceStaleOrUnbound)
                        .Equals(laneName, StringComparison.Ordinal))
                {
                    continue;
                }

                int? candidate = OptionalInteger(
                    binding,
                    "maximumCurrentRunAgeMinutes",
                    GateReason.EvidenceStaleOrUnbound);
                bool currentOverrideDeclared = candidate is not null;
                if (overrideDeclared is not null && overrideDeclared.Value != currentOverrideDeclared)
                {
                    throw new GateValidationException(
                        GateReason.EvidenceStaleOrUnbound,
                        "lane-current-run-age-presence");
                }

                overrideDeclared = currentOverrideDeclared;

                if (candidate is not null
                    && candidate.Value < globalCeiling)
                {
                    throw new GateValidationException(
                        GateReason.EvidenceStaleOrUnbound,
                        "lane-current-run-age-below-global");
                }

                if (candidate is not null
                    && candidate.Value > maximumLaneCeiling)
                {
                    throw new GateValidationException(
                        GateReason.EvidenceStaleOrUnbound,
                        "lane-current-run-age-above-maximum");
                }

                if (candidate is not null && resolved is not null && resolved.Value != candidate.Value)
                {
                    throw new GateValidationException(
                        GateReason.EvidenceStaleOrUnbound,
                        "lane-current-run-age-disagreement");
                }

                resolved = candidate ?? resolved;
            }
        }

        return resolved ?? globalCeiling;
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
        => RejectForbiddenEvidenceFields(
            node,
            ["secret", "password", "credential", "payload", "prompt", "token"]);

    /// <summary>Validates an artifact against the policy's metadata-only field and value bounds.</summary>
    /// <param name="node">The artifact to inspect.</param>
    /// <param name="policy">The policy that governs retained metadata.</param>
    internal static void ValidateMetadataOnly(JsonNode node, JsonObject policy)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(policy);
        JsonObject metadata = RequiredObject(policy, "metadataOnly", GateReason.EvidencePayloadForbidden);
        IReadOnlyList<string> forbidden = RequiredStrings(
            metadata,
            "forbiddenFieldNames",
            GateReason.EvidencePayloadForbidden);
        int maximumStringLength = RequiredInteger(
            metadata,
            "maximumStringLength",
            GateReason.EvidencePayloadForbidden);
        RejectForbiddenEvidenceFields(node, forbidden);
        RejectUnsafeMetadataValues(node, maximumStringLength);
    }

    private static void RejectForbiddenEvidenceFields(JsonNode node, IReadOnlyList<string> forbiddenNames)
    {
        if (node is JsonObject value)
        {
            foreach ((string name, JsonNode? child) in value)
            {
                string normalized = name.ToLowerInvariant();
                if (forbiddenNames.Any(forbidden => normalized.Contains(forbidden, StringComparison.Ordinal)))
                {
                    throw new GateValidationException(GateReason.EvidencePayloadForbidden, name);
                }

                if (child is not null)
                {
                    RejectForbiddenEvidenceFields(child, forbiddenNames);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array)
            {
                if (child is not null)
                {
                    RejectForbiddenEvidenceFields(child, forbiddenNames);
                }
            }
        }
    }

    private static void RejectUnsafeMetadataValues(JsonNode node)
        => RejectUnsafeMetadataValues(node, maximumStringLength: 512);

    private static void RejectUnsafeMetadataValues(JsonNode node, int maximumStringLength)
    {
        if (node is JsonObject value)
        {
            foreach ((string name, JsonNode? child) in value)
            {
                if (child is JsonValue scalar && scalar.TryGetValue(out string? text))
                {
                    if (text.Length > maximumStringLength
                        || text.Any(char.IsControl)
                        || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
                        || Regexes.UnsafeCredential().IsMatch(text))
                    {
                        throw new GateValidationException(GateReason.EvidencePayloadForbidden, name);
                    }
                }
                else if (child is not null)
                {
                    RejectUnsafeMetadataValues(child, maximumStringLength);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array)
            {
                if (child is JsonValue scalar && scalar.TryGetValue(out string? text))
                {
                    if (text.Length > maximumStringLength
                        || text.Any(char.IsControl)
                        || text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
                        || Regexes.UnsafeCredential().IsMatch(text))
                    {
                        throw new GateValidationException(GateReason.EvidencePayloadForbidden, "metadata-value");
                    }
                }
                else if (child is not null)
                {
                    RejectUnsafeMetadataValues(child, maximumStringLength);
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
