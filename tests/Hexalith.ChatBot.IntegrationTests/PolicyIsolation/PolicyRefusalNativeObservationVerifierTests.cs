using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.PolicyIsolation;

/// <summary>Synthetic adversarial contract tests, never native-store or independent witness evidence.</summary>
public sealed class PolicyRefusalNativeObservationVerifierTests
{
    /// <summary>A complete correctly signed synthetic pair is accepted only for the narrow unchanged-state predicate.</summary>
    [Fact]
    public void CompleteIndependentlySignedPairMustMatchTheExactRun()
    {
        using RSA witness = RSA.Create(3072);
        JsonObject expected = Expected();
        JsonObject before = Observation(expected, "before");
        byte[] first = Sign(before, witness);
        JsonObject after = Observation(expected, "after");
        after["beforeEnvelopeSha256"] = PolicyRefusalNativeObservationVerifier.Hash(first);
        PolicyRefusalNativeObservationVerifier.Verify(Element(expected), first, Sign(after, witness), witness, "independent-observer");
    }

    /// <summary>A valid signed before observation is accepted before any submission begins.</summary>
    [Fact]
    public void CompleteIndependentlySignedBeforeObservationMustBeAccepted()
    {
        using RSA witness = RSA.Create(3072);
        JsonObject expected = Expected();
        PolicyRefusalNativeObservationVerifier.VerifyBefore(Element(expected), Sign(Observation(expected, "before"), witness), witness, "independent-observer");
    }

    /// <summary>Signed but incomplete, stale, foreign or mutating observations never prove absence.</summary>
    [Theory]
    [InlineData("wrong-backing")]
    [InlineData("wrong-source")]
    [InlineData("wrong-run")]
    [InlineData("wrong-config")]
    [InlineData("missing-control")]
    [InlineData("control-not-present")]
    [InlineData("same-control")]
    [InlineData("unverified-control")]
    [InlineData("uncommitted-control")]
    [InlineData("prefix-filter")]
    [InlineData("incomplete")]
    [InlineData("wrong-count")]
    [InlineData("duplicate-key")]
    [InlineData("mutation")]
    [InlineData("new-key")]
    [InlineData("deleted-key")]
    [InlineData("wrong-order")]
    [InlineData("wrong-chain")]
    [InlineData("producer-observer")]
    [InlineData("forged-signature")]
    [InlineData("missing-field")]
    [InlineData("unknown-field")]
    [InlineData("empty-database")]
    [InlineData("null-binding")]
    [InlineData("missing-causes")]
    [InlineData("generic-http-cause")]
    [InlineData("wrong-cause-command")]
    [InlineData("wrong-cause-correlation")]
    [InlineData("wrong-cause-tenant")]
    [InlineData("wrong-cause-principal")]
    [InlineData("wrong-cause-task")]
    [InlineData("wrong-cause-body")]
    [InlineData("producer-cause")]
    [InlineData("wrong-runtime-hash")]
    [InlineData("loaded-artifacts-unverified")]
    public void IncompleteOrMismatchedNativeEvidenceMustRemainBlocked(string scenario)
    {
        using RSA witness = RSA.Create(3072);
        using RSA producer = RSA.Create(3072);
        JsonObject expected = Expected();
        JsonObject before = Observation(expected, "before");
        byte[] first = Sign(before, witness);
        JsonObject after = Observation(expected, "after");
        after["beforeEnvelopeSha256"] = PolicyRefusalNativeObservationVerifier.Hash(first);
        JsonObject scope = after["scope"]!.AsObject();
        JsonArray keys = scope["keys"]!.AsArray();
        JsonArray controls = after["positiveControls"]!.AsArray();
        switch (scenario)
        {
            case "wrong-backing": after["backing"]!["redisRunId"] = new string('9', 40); break;
            case "wrong-source": after["sourceSha256"] = Hash('9'); break;
            case "wrong-run": after["runId"] = "another-run"; break;
            case "wrong-config": after["configurationSha256"] = Hash('9'); break;
            case "missing-control": controls.RemoveAt(1); break;
            case "control-not-present": controls[0]!["keySha256"] = Hash('9'); break;
            case "same-control": controls[1]!["keySha256"] = Hash('1'); break;
            case "unverified-control": controls[0]!["ownerReadbackVerified"] = false; break;
            case "uncommitted-control": controls[0]!["committedEventSequence"] = 0; break;
            case "prefix-filter": scope["kind"] = "prefix-filtered"; break;
            case "incomplete": scope["complete"] = false; break;
            case "wrong-count": scope["dbSize"] = 3; break;
            case "duplicate-key": keys.Add(keys[0]!.DeepClone()); scope["dbSize"] = 3; break;
            case "mutation": keys[0]!["valueSha256"] = Hash('9'); break;
            case "new-key": keys.Add(new JsonObject { ["keySha256"] = Hash('9'), ["valueSha256"] = Hash('8') }); scope["dbSize"] = 3; break;
            case "deleted-key": keys.RemoveAt(0); scope["dbSize"] = 1; break;
            case "wrong-order": after["captureStartedUtc"] = "2026-10-31T10:00:01Z"; break;
            case "wrong-chain": after["beforeEnvelopeSha256"] = Hash('9'); break;
            case "producer-observer": after["observerId"] = "producer"; break;
            case "missing-field": after.Remove("backing"); break;
            case "unknown-field": after["protectedValues"] = "synthetic-disallowed-field"; break;
            case "empty-database": keys.Clear(); scope["dbSize"] = 0; controls.Clear(); break;
            case "null-binding": expected["sourceSha256"] = null; break;
            case "missing-causes": after.Remove("policyGateCauses"); break;
            case "generic-http-cause": after["policyGateCauses"]![0]!["reasonCode"] = "dependency_degraded"; break;
            case "wrong-cause-command": after["policyGateCauses"]![0]!["commandId"] = "another-command"; break;
            case "wrong-cause-correlation": after["policyGateCauses"]![0]!["correlationId"] = "another-correlation"; break;
            case "wrong-cause-tenant": after["policyGateCauses"]![0]!["tenantId"] = "tenant-foreign"; break;
            case "wrong-cause-principal": after["policyGateCauses"]![0]!["principalId"] = "actor-foreign"; break;
            case "wrong-cause-task": after["policyGateCauses"]![0]!["taskId"] = "another-task"; break;
            case "wrong-cause-body": after["policyGateCauses"]![0]!["requestBodySha256"] = Hash('9'); break;
            case "producer-cause": after["policyGateCauses"]![0]!["sourceKind"] = "producer-http-receipt"; break;
            case "wrong-runtime-hash": after["runtimeArtifacts"]![0]!["sha256"] = Hash('0'); break;
            case "loaded-artifacts-unverified": after["actualLoadedArtifactsVerified"] = false; break;
        }
        byte[] last = Sign(after, scenario == "forged-signature" ? producer : witness);
        Should.Throw<InvalidOperationException>(() => PolicyRefusalNativeObservationVerifier.Verify(
            Element(expected), first, last, witness, "independent-observer"));
    }

    /// <summary>A failed independent before observation prevents the producer from sending its requests.</summary>
    [Theory]
    [InlineData("producer")]
    [InlineData("signature")]
    [InlineData("control")]
    [InlineData("foreign-run")]
    [InlineData("wrong-runtime")]
    [InlineData("loaded-artifacts-unverified")]
    [InlineData("wrong-request-body")]
    public void BeforeObservationMustBeIndependentAndBoundBeforeSubmission(string scenario)
    {
        using RSA witness = RSA.Create(3072);
        using RSA other = RSA.Create(3072);
        JsonObject expected = Expected();
        JsonObject before = Observation(expected, "before");
        if (scenario == "producer") { before["observerId"] = "producer"; }
        if (scenario == "control") { before["positiveControls"]!.AsArray().Clear(); }
        if (scenario == "foreign-run") { before["runId"] = "foreign-run"; }
        if (scenario == "wrong-runtime") { before["runtimeArtifacts"]![0]!["sha256"] = Hash('0'); }
        if (scenario == "loaded-artifacts-unverified") { before["actualLoadedArtifactsVerified"] = false; }
        if (scenario == "wrong-request-body") { before["submissions"]![0]!["requestBodySha256"] = Hash('0'); }
        byte[] envelope = Sign(before, scenario == "signature" ? other : witness);
        Should.Throw<InvalidOperationException>(() => PolicyRefusalNativeObservationVerifier.VerifyBefore(Element(expected), envelope, witness, "independent-observer"));
    }

    /// <summary>A producer key and a producer identity cannot be promoted into independent approval.</summary>
    [Fact]
    public void ProducerIdentityMustNotActAsItsOwnWitness()
    {
        using RSA producer = RSA.Create(3072);
        JsonObject expected = Expected();
        JsonObject before = Observation(expected, "before");
        before["observerId"] = "producer";
        byte[] first = Sign(before, producer);
        JsonObject after = Observation(expected, "after");
        after["observerId"] = "producer";
        after["beforeEnvelopeSha256"] = PolicyRefusalNativeObservationVerifier.Hash(first);
        Should.Throw<InvalidOperationException>(() => PolicyRefusalNativeObservationVerifier.Verify(
            Element(expected), first, Sign(after, producer), producer, "producer"));
    }

    private static JsonObject Expected() => new()
    {
        ["schema"] = "hexalith.chatbot.policy-refusal-native.v1", ["rootRevision"] = new string('a', 40),
        ["sourceSha256"] = Hash('a'), ["configurationSha256"] = Hash('b'), ["requestSha256"] = Hash('c'),
        ["runId"] = "synthetic-run", ["producerId"] = "producer",
        ["backing"] = new JsonObject { ["containerId"] = Hash('d'), ["redisRunId"] = new string('e', 40), ["imageDigest"] = "sha256:" + Hash('f'), ["database"] = 0 },
        ["runStartedUtc"] = "2026-10-31T10:00:00Z", ["submissionStartedUtc"] = "2026-10-31T10:00:03Z",
        ["submissionEndedUtc"] = "2026-10-31T10:00:04Z", ["runEndedUtc"] = "2026-10-31T10:00:07Z",
        ["tenants"] = new JsonArray(new JsonObject { ["tenantId"] = "tenant-alpha", ["principalId"] = "actor-alpha" },
            new JsonObject { ["tenantId"] = "tenant-beta", ["principalId"] = "actor-beta" }),
        ["submissions"] = new JsonArray(Enumerable.Range(0, 2).Select(index => (JsonNode)new JsonObject
        {
            ["tenantId"] = index == 0 ? "tenant-alpha" : "tenant-beta", ["principalId"] = index == 0 ? "actor-alpha" : "actor-beta",
            ["commandType"] = "SubmitTenantPolicyChange", ["commandId"] = "synthetic-command-" + index,
            ["correlationId"] = "synthetic-correlation-" + index, ["taskId"] = "synthetic-task-" + index,
            ["requestBodySha256"] = Hash(index == 0 ? '5' : '6'), ["method"] = "POST", ["path"] = "/api/v1/commands",
            ["contentType"] = "application/json; charset=utf-8", ["origin"] = "ui", ["requestSchemaVersion"] = "v1",
        }).ToArray()),
        ["runtimeArtifacts"] = new JsonArray(new[] { "producer", "apphost", "chatbot" }.Select(resource => (JsonNode)new JsonObject
        {
            ["resourceId"] = resource, ["path"] = "/synthetic/" + resource + ".dll", ["sha256"] = Hash('7'), ["bytes"] = 100,
        }).ToArray()),
    };

    private static JsonObject Observation(JsonObject expected, string phase)
    {
        JsonObject result = new();
        foreach (string key in new[] { "schema", "rootRevision", "sourceSha256", "configurationSha256", "requestSha256", "runId", "producerId", "backing", "submissions", "runtimeArtifacts" })
        {
            result[key] = expected[key]!.DeepClone();
        }
        result["phase"] = phase;
        result["observerId"] = "independent-observer";
        result["actualLoadedArtifactsVerified"] = true;
        result["captureStartedUtc"] = phase == "before" ? "2026-10-31T10:00:01Z" : "2026-10-31T10:00:05Z";
        result["captureEndedUtc"] = phase == "before" ? "2026-10-31T10:00:02Z" : "2026-10-31T10:00:06Z";
        result["scope"] = new JsonObject
        {
            ["kind"] = "entire-disposable-redis-database", ["algorithm"] = "atomic-keys-dump-v1", ["complete"] = true, ["database"] = 0, ["dbSize"] = 2,
            ["keys"] = new JsonArray(new JsonObject { ["keySha256"] = Hash('1'), ["valueSha256"] = Hash('3') }, new JsonObject { ["keySha256"] = Hash('2'), ["valueSha256"] = Hash('4') }),
        };
        result["positiveControls"] = new JsonArray(Enumerable.Range(0, 2).Select(index => (JsonNode)new JsonObject
        {
            ["tenantId"] = expected["tenants"]![index]!["tenantId"]!.DeepClone(), ["principalId"] = expected["tenants"]![index]!["principalId"]!.DeepClone(),
            ["keySha256"] = Hash(index == 0 ? '1' : '2'), ["ownerReadbackVerified"] = true, ["committedEventSequence"] = 2, ["membershipRole"] = "TenantOwner",
        }).ToArray());
        if (phase == "after")
        {
            result["policyGateCauses"] = new JsonArray(expected["submissions"]!.AsArray().Select(request =>
            {
                JsonObject cause = new();
                foreach (string field in new[] { "tenantId", "principalId", "commandId", "commandType", "correlationId", "taskId", "requestBodySha256" })
                {
                    cause[field] = request![field]!.DeepClone();
                }
                cause["reasonCode"] = "policy-snapshot-unavailable"; cause["owner"] = "ChatBot";
                cause["sourceKind"] = "chatbot-owned-correlated-admission-observation";
                cause["evidenceSha256"] = Hash('8'); cause["observedAtUtc"] = "2026-10-31T10:00:05.5Z";
                return (JsonNode)cause;
            }).ToArray());
        }
        return result;
    }

    private static JsonElement Element(JsonObject value) => JsonSerializer.SerializeToElement(value);

    private static byte[] Sign(JsonObject payload, RSA key)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        return JsonSerializer.SerializeToUtf8Bytes(new { payloadBase64 = Convert.ToBase64String(bytes),
            signatureBase64 = Convert.ToBase64String(key.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss)) });
    }

    private static string Hash(char character) => new(character, 64);
}
