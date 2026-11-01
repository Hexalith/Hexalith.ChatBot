using System.Security.Cryptography;
using System.Text.Json;

namespace Hexalith.ChatBot.IntegrationTests.PolicyIsolation;

/// <summary>Consumes independently signed native observations; source tests confer no witness acceptance.</summary>
internal static class PolicyRefusalNativeObservationVerifier
{
    /// <summary>Checks the enrolled independent before observation before any policy request is sent.</summary>
    internal static void VerifyBefore(JsonElement expected, byte[] beforeEnvelope, RSA enrolledObserverKey, string enrolledObserverId)
    {
        ArgumentNullException.ThrowIfNull(enrolledObserverKey);
        try
        {
            ValidateExpected(expected);
            using JsonDocument document = Open(beforeEnvelope, enrolledObserverKey);
            JsonElement before = document.RootElement;
            Require(enrolledObserverKey.KeySize >= 3072 && !string.IsNullOrWhiteSpace(enrolledObserverId)
                && enrolledObserverId != expected.GetProperty("producerId").GetString()
                && before.GetProperty("observerId").GetString() == enrolledObserverId
                && before.GetProperty("phase").GetString() == "before", "before-observer");
            foreach (string field in new[] { "schema", "rootRevision", "sourceSha256", "runId", "producerId", "configurationSha256", "requestSha256", "backing", "submissions", "runtimeArtifacts" })
            {
                Require(Same(expected.GetProperty(field), before.GetProperty(field)), "before-run-binding");
            }
            DateTimeOffset start = before.GetProperty("captureStartedUtc").GetDateTimeOffset();
            DateTimeOffset end = before.GetProperty("captureEndedUtc").GetDateTimeOffset();
            Require(start.Offset == TimeSpan.Zero && end.Offset == TimeSpan.Zero
                && expected.GetProperty("runStartedUtc").GetDateTimeOffset() <= start && start <= end && end <= DateTimeOffset.UtcNow, "before-capture-order");
            PositiveControls(expected, before, Inventory(before));
            Require(before.GetProperty("actualLoadedArtifactsVerified").GetBoolean(), "before-actual-loaded-artifacts");
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException
            or FormatException or CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException("Native refusal evidence is blocked: independent before observation unavailable or invalid.", exception);
        }
    }

    /// <summary>Requires a complete unchanged native database, independently witnessed positive controls and exact run binding.</summary>
    internal static void Verify(JsonElement expected, byte[] beforeEnvelope, byte[] afterEnvelope,
        RSA enrolledObserverKey, string enrolledObserverId)
    {
        ArgumentNullException.ThrowIfNull(beforeEnvelope);
        ArgumentNullException.ThrowIfNull(afterEnvelope);
        ArgumentNullException.ThrowIfNull(enrolledObserverKey);
        try
        {
            ValidateExpected(expected);
            using JsonDocument before = Open(beforeEnvelope, enrolledObserverKey);
            using JsonDocument after = Open(afterEnvelope, enrolledObserverKey);
            JsonElement first = before.RootElement;
            JsonElement last = after.RootElement;
            Require(enrolledObserverKey.KeySize >= 3072, "observer-key");
            Require(enrolledObserverId != expected.GetProperty("producerId").GetString(), "observer-independence");
            Require(enrolledObserverId == first.GetProperty("observerId").GetString()
                && enrolledObserverId == last.GetProperty("observerId").GetString(), "observer-identity");
            Require(first.GetProperty("phase").GetString() == "before" && last.GetProperty("phase").GetString() == "after", "phase");
            foreach (string field in new[] { "schema", "rootRevision", "sourceSha256", "runId", "producerId", "configurationSha256", "requestSha256", "submissions", "runtimeArtifacts" })
            {
                Require(Same(expected.GetProperty(field), first.GetProperty(field))
                    && Same(expected.GetProperty(field), last.GetProperty(field)), "run-binding");
            }
            Require(expected.GetProperty("schema").GetString() == "hexalith.chatbot.policy-refusal-native.v1", "schema");
            foreach (string field in new[] { "containerId", "redisRunId", "imageDigest", "database" })
            {
                Require(Same(expected.GetProperty("backing").GetProperty(field), first.GetProperty("backing").GetProperty(field))
                    && Same(expected.GetProperty("backing").GetProperty(field), last.GetProperty("backing").GetProperty(field)), "native-backing");
            }
            Require(expected.GetProperty("backing").GetProperty("database").GetInt32() == 0, "database");
            Require(last.GetProperty("beforeEnvelopeSha256").GetString() == Hash(beforeEnvelope), "observation-chain");
            DateTimeOffset runStarted = expected.GetProperty("runStartedUtc").GetDateTimeOffset();
            DateTimeOffset submitStarted = expected.GetProperty("submissionStartedUtc").GetDateTimeOffset();
            DateTimeOffset submitEnded = expected.GetProperty("submissionEndedUtc").GetDateTimeOffset();
            DateTimeOffset runEnded = expected.GetProperty("runEndedUtc").GetDateTimeOffset();
            DateTimeOffset firstStart = first.GetProperty("captureStartedUtc").GetDateTimeOffset();
            DateTimeOffset firstEnd = first.GetProperty("captureEndedUtc").GetDateTimeOffset();
            DateTimeOffset lastStart = last.GetProperty("captureStartedUtc").GetDateTimeOffset();
            DateTimeOffset lastEnd = last.GetProperty("captureEndedUtc").GetDateTimeOffset();
            Require(new[] { runStarted, submitStarted, submitEnded, runEnded, firstStart, firstEnd, lastStart, lastEnd }
                .All(time => time.Offset == TimeSpan.Zero), "utc");
            Require(runStarted <= firstStart && firstStart <= firstEnd && firstEnd <= submitStarted
                && submitStarted <= submitEnded && submitEnded <= lastStart && lastStart <= lastEnd && lastEnd <= runEnded, "capture-order");
            Dictionary<string, string> initial = Inventory(first);
            Dictionary<string, string> final = Inventory(last);
            Require(initial.Count == final.Count && initial.All(entry => final.TryGetValue(entry.Key, out string? digest) && digest == entry.Value), "native-mutation-or-uncertainty");
            PositiveControls(expected, first, initial);
            PositiveControls(expected, last, final);
            Require(first.GetProperty("actualLoadedArtifactsVerified").GetBoolean()
                && last.GetProperty("actualLoadedArtifactsVerified").GetBoolean(), "actual-loaded-artifacts");
            PolicyGateCauses(expected, last);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException
            or FormatException or CryptographicException or ArgumentException)
        {
            // Raw input and provider contents never enter diagnostics or retained receipts.
            throw new InvalidOperationException("Native refusal evidence is blocked: missing, mismatched, incomplete or mutated independent observation.", exception);
        }
    }

    /// <summary>Hashes exact envelope or implementation bytes without exposing native contents.</summary>
    internal static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static JsonDocument Open(byte[] bytes, RSA key)
    {
        Require(bytes.Length is > 0 and <= 16 * 1024 * 1024, "envelope-size");
        using JsonDocument envelope = JsonDocument.Parse(bytes);
        JsonProperty[] properties = envelope.RootElement.EnumerateObject().ToArray();
        Require(properties.Length == 2 && properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == 2, "envelope-fields");
        byte[] payload = Convert.FromBase64String(envelope.RootElement.GetProperty("payloadBase64").GetString()!);
        byte[] signature = Convert.FromBase64String(envelope.RootElement.GetProperty("signatureBase64").GetString()!);
        Require(key.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss), "independent-signature");
        JsonDocument document = JsonDocument.Parse(payload);
        try
        {
            JsonElement root = document.RootElement;
            RejectDuplicateProperties(root);
            string[] fields = ["schema", "rootRevision", "sourceSha256", "runId", "producerId", "configurationSha256", "requestSha256",
                "backing", "phase", "observerId", "captureStartedUtc", "captureEndedUtc", "scope", "positiveControls",
                "submissions", "runtimeArtifacts", "actualLoadedArtifactsVerified"];
            Fields(root, root.GetProperty("phase").GetString() == "after" ? [.. fields, "beforeEnvelopeSha256", "policyGateCauses"] : fields);
            Fields(root.GetProperty("backing"), ["containerId", "redisRunId", "imageDigest", "database"]);
            return document;
        }
        catch { document.Dispose(); throw; }
    }

    private static Dictionary<string, string> Inventory(JsonElement observation)
    {
        JsonElement scope = observation.GetProperty("scope");
        Fields(scope, ["kind", "algorithm", "complete", "database", "dbSize", "keys"]);
        Require(scope.GetProperty("kind").GetString() == "entire-disposable-redis-database"
            && scope.GetProperty("algorithm").GetString() == "atomic-keys-dump-v1"
            && scope.GetProperty("complete").GetBoolean()
            && scope.GetProperty("database").GetInt32() == 0, "complete-native-scope");
        Dictionary<string, string> entries = new(StringComparer.Ordinal);
        foreach (JsonElement entry in scope.GetProperty("keys").EnumerateArray())
        {
            Fields(entry, ["keySha256", "valueSha256"]);
            string key = entry.GetProperty("keySha256").GetString()!;
            string value = entry.GetProperty("valueSha256").GetString()!;
            Require(IsHash(key) && IsHash(value) && entries.TryAdd(key, value), "inventory-entry");
        }
        Require(entries.Count > 0 && entries.Count == scope.GetProperty("dbSize").GetInt32(), "inventory-completeness");
        return entries;
    }

    private static void PositiveControls(JsonElement expected, JsonElement observation, Dictionary<string, string> inventory)
    {
        JsonElement[] tenants = expected.GetProperty("tenants").EnumerateArray().ToArray();
        JsonElement[] controls = observation.GetProperty("positiveControls").EnumerateArray().ToArray();
        Require(tenants.Length == 2 && controls.Length == 2
            && tenants.Select(tenant => tenant.GetProperty("tenantId").GetString()).Distinct(StringComparer.Ordinal).Count() == 2
            && tenants.Select(tenant => tenant.GetProperty("principalId").GetString()).Distinct(StringComparer.Ordinal).Count() == 2, "two-positive-tenants");
        foreach (JsonElement tenant in tenants)
        {
            JsonElement[] matching = controls.Where(control => Same(control.GetProperty("tenantId"), tenant.GetProperty("tenantId"))
                && Same(control.GetProperty("principalId"), tenant.GetProperty("principalId"))).ToArray();
            Require(matching.Length == 1, "positive-control-binding");
            JsonElement control = matching[0];
            Fields(control, ["tenantId", "principalId", "ownerReadbackVerified", "committedEventSequence", "membershipRole", "keySha256"]);
            Require(control.GetProperty("ownerReadbackVerified").GetBoolean()
                && control.GetProperty("committedEventSequence").GetInt64() > 0
                && control.GetProperty("membershipRole").GetString() == "TenantOwner"
                && inventory.ContainsKey(control.GetProperty("keySha256").GetString()!), "native-positive-control");
        }
        Require(controls.Select(control => control.GetProperty("keySha256").GetString()).Distinct(StringComparer.Ordinal).Count() == 2, "distinct-native-controls");
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                Require(names.Add(property.Name), "duplicate-property");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray()) { RejectDuplicateProperties(item); }
        }
    }

    private static bool Same(JsonElement left, JsonElement right) => JsonElement.DeepEquals(left, right);

    private static void PolicyGateCauses(JsonElement expected, JsonElement observation)
    {
        JsonElement[] requests = expected.GetProperty("submissions").EnumerateArray().ToArray();
        JsonElement[] causes = observation.GetProperty("policyGateCauses").EnumerateArray().ToArray();
        Require(causes.Length == 2, "exact-policy-gate-causes-missing");
        foreach (JsonElement request in requests)
        {
            JsonElement[] matching = causes.Where(cause => Same(cause.GetProperty("commandId"), request.GetProperty("commandId"))).ToArray();
            Require(matching.Length == 1, "policy-cause-command");
            JsonElement cause = matching[0];
            Fields(cause, ["tenantId", "principalId", "commandId", "commandType", "correlationId", "taskId", "requestBodySha256",
                "reasonCode", "owner", "sourceKind", "evidenceSha256", "observedAtUtc"]);
            foreach (string field in new[] { "tenantId", "principalId", "commandId", "commandType", "correlationId", "taskId", "requestBodySha256" })
            {
                Require(Same(cause.GetProperty(field), request.GetProperty(field)), "exact-policy-cause-correlation");
            }
            Require(cause.GetProperty("reasonCode").GetString() == "policy-snapshot-unavailable"
                && cause.GetProperty("owner").GetString() == "ChatBot"
                && cause.GetProperty("sourceKind").GetString() == "chatbot-owned-correlated-admission-observation"
                && IsHash(cause.GetProperty("evidenceSha256").GetString()), "independent-chatbot-owned-cause");
            DateTimeOffset observed = cause.GetProperty("observedAtUtc").GetDateTimeOffset();
            Require(observed.Offset == TimeSpan.Zero && observation.GetProperty("captureStartedUtc").GetDateTimeOffset() <= observed
                && observed <= observation.GetProperty("captureEndedUtc").GetDateTimeOffset(), "cause-observation-order");
        }
    }

    private static void ValidateExpected(JsonElement expected)
    {
        Require(expected.GetProperty("schema").GetString() == "hexalith.chatbot.policy-refusal-native.v1", "expected-schema");
        foreach (string field in new[] { "sourceSha256", "configurationSha256", "requestSha256" })
        {
            Require(IsHash(expected.GetProperty(field).GetString()!), "expected-digest");
        }
        string? revision = expected.GetProperty("rootRevision").GetString();
        Require(revision is { Length: 40 } && revision.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'), "expected-candidate");
        JsonElement backing = expected.GetProperty("backing");
        Fields(backing, ["containerId", "redisRunId", "imageDigest", "database"]);
        string? redisRun = backing.GetProperty("redisRunId").GetString();
        string? image = backing.GetProperty("imageDigest").GetString();
        Require(IsHash(backing.GetProperty("containerId").GetString()) && redisRun is { Length: 40 }
            && redisRun.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
            && image is not null && image.StartsWith("sha256:", StringComparison.Ordinal) && IsHash(image[7..])
            && backing.GetProperty("database").GetInt32() == 0, "expected-backing");
        Require(!string.IsNullOrWhiteSpace(expected.GetProperty("runId").GetString())
            && !string.IsNullOrWhiteSpace(expected.GetProperty("producerId").GetString()), "expected-run");
        JsonElement[] submissions = expected.GetProperty("submissions").EnumerateArray().ToArray();
        Require(submissions.Length == 2, "expected-submissions");
        foreach (JsonElement submission in submissions)
        {
            Fields(submission, ["tenantId", "principalId", "commandId", "commandType", "correlationId", "taskId", "requestBodySha256",
                "method", "path", "contentType", "origin", "requestSchemaVersion"]);
            Require(submission.GetProperty("commandType").GetString() == "SubmitTenantPolicyChange"
                && submission.GetProperty("method").GetString() == "POST" && submission.GetProperty("path").GetString() == "/api/v1/commands"
                && submission.GetProperty("contentType").GetString() == "application/json; charset=utf-8"
                && submission.GetProperty("origin").GetString() == "ui" && submission.GetProperty("requestSchemaVersion").GetString() == "v1"
                && IsHash(submission.GetProperty("requestBodySha256").GetString()), "expected-exact-request");
            foreach (string field in new[] { "tenantId", "principalId", "commandId", "correlationId", "taskId" })
            {
                Require(!string.IsNullOrWhiteSpace(submission.GetProperty(field).GetString()), "expected-request-identity");
            }
        }
        foreach (string field in new[] { "tenantId", "principalId", "commandId", "correlationId", "taskId" })
        {
            Require(submissions.Select(submission => submission.GetProperty(field).GetString()).Distinct(StringComparer.Ordinal).Count() == 2, "distinct-exact-requests");
        }
        foreach (JsonElement tenant in expected.GetProperty("tenants").EnumerateArray())
        {
            Require(submissions.Count(submission => Same(submission.GetProperty("tenantId"), tenant.GetProperty("tenantId"))
                && Same(submission.GetProperty("principalId"), tenant.GetProperty("principalId"))) == 1, "request-tenant-principal-binding");
        }
        JsonElement[] artifacts = expected.GetProperty("runtimeArtifacts").EnumerateArray().ToArray();
        Require(artifacts.Length >= 3, "expected-runtime-artifacts");
        HashSet<(string?, string?)> seenArtifacts = [];
        foreach (JsonElement artifact in artifacts)
        {
            Fields(artifact, ["resourceId", "path", "sha256", "bytes"]);
            string? resource = artifact.GetProperty("resourceId").GetString();
            string? path = artifact.GetProperty("path").GetString();
            Require(!string.IsNullOrWhiteSpace(resource) && path is not null && Path.IsPathFullyQualified(path)
                && seenArtifacts.Add((resource, path)) && IsHash(artifact.GetProperty("sha256").GetString())
                && artifact.GetProperty("bytes").GetInt64() > 0, "expected-runtime-artifact-identity");
        }
        Require(artifacts.Any(artifact => artifact.GetProperty("resourceId").GetString() == "producer")
            && artifacts.Any(artifact => artifact.GetProperty("resourceId").GetString() == "apphost")
            && artifacts.Any(artifact => artifact.GetProperty("resourceId").GetString() == "chatbot"), "expected-producer-and-child-artifacts");
    }

    private static void Fields(JsonElement value, string[] required)
    {
        string[] actual = value.EnumerateObject().Select(property => property.Name).ToArray();
        Require(actual.Length == required.Length && actual.ToHashSet(StringComparer.Ordinal).SetEquals(required), "closed-wire-schema");
    }

    private static bool IsHash(string? value) => value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void Require(bool condition, string reason)
    {
        if (!condition) { throw new InvalidOperationException("Native observation refused: " + reason); }
    }
}
