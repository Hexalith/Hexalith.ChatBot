using System.Security.Cryptography;
using System.Text.Json;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.PolicyIsolation;

/// <summary>The opt-in producer and independent-observer handoff; never a policy persistence or A6 acceptance switch.</summary>
public sealed class PolicyRefusalPreflightTests(ITestOutputHelper output)
{
    /// <summary>Requires real owner preparation, both canonical refusals and independently signed unchanged native state.</summary>
    [Fact(Timeout = 900_000)]
    [Trait("Category", "PolicyRefusalNativePreflight")]
    public async Task MissingProofMustRefuseBothRealPrincipalsWithoutNativeMutation()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("HEXALITH_POLICY_REFUSAL_PREFLIGHT") == "1",
            "Native refusal producer is not selected; this skip supplies no evidence or approval.");
        string? trustPath = Environment.GetEnvironmentVariable("HEXALITH_POLICY_REFUSAL_OBSERVER_TRUST");
        if (string.IsNullOrWhiteSpace(trustPath) || !File.Exists(trustPath))
        {
            throw new InvalidOperationException("Preflight blocked: independently enrolled observer trust file is required; no witness was fabricated.");
        }
        using JsonDocument trust = JsonDocument.Parse(await File.ReadAllBytesAsync(trustPath, TestContext.Current.CancellationToken).ConfigureAwait(true));
        string observer = trust.RootElement.GetProperty("observerId").GetString()!;
        string authority = trust.RootElement.GetProperty("authorizedBy").GetString()!;
        if (string.IsNullOrWhiteSpace(observer) || observer == "chatbot-policy-refusal-producer" || string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException("Preflight blocked: independent observer enrollment is absent.");
        }
        using RSA witness = RSA.Create();
        string publicKey = trust.RootElement.GetProperty("publicKeyPem").GetString()!;
        if (!publicKey.StartsWith("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal)
            || publicKey.Contains("PRIVATE KEY", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Preflight blocked: observer enrollment must contain a public key only.");
        }
        witness.ImportFromPem(publicKey);
        if (witness.KeySize < 3072) { throw new InvalidOperationException("Preflight blocked: observer key is outside the proposed RSA-PSS profile."); }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(14));
        CancellationToken token = deadline.Token;
        PolicyRefusalPreflightTopology topology = await PolicyRefusalPreflightTopology.StartAsync(token).ConfigureAwait(true);
        await using var topologyLifetime = topology.ConfigureAwait(true);
        output.WriteLine("Independent observer exchange directory: {0}", topology.DirectoryPath);
        await topology.SealRequestAsync(token).ConfigureAwait(true);
        await topology.WriteAsync("before-request.json", topology.Request, token).ConfigureAwait(true);
        byte[] before = await topology.WaitForObserverAsync("before", token).ConfigureAwait(true);
        PolicyRefusalNativeObservationVerifier.VerifyBefore(JsonSerializer.SerializeToElement(topology.Request), before, witness, observer);
        await topology.RecheckBindingAsync(token).ConfigureAwait(true);
        topology.Request["submissionStartedUtc"] = DateTimeOffset.UtcNow;
        await topology.ObserveHttpDependencyAsync("tenant-alpha", "actor-alpha", token).ConfigureAwait(true);
        await topology.ObserveHttpDependencyAsync("tenant-beta", "actor-beta", token).ConfigureAwait(true);
        topology.Request["submissionEndedUtc"] = DateTimeOffset.UtcNow;
        topology.Request["beforeEnvelopeSha256"] = PolicyRefusalNativeObservationVerifier.Hash(before);
        await topology.WriteAsync("after-request.json", topology.Request, token).ConfigureAwait(true);
        // The current candidate supplies no supported cross-process cause reader. Generic HTTP observations
        // and a native unchanged-state pair cannot establish which admission branch produced the responses.
        PolicyRefusalPreflightTopology.RequirePolicyGateCauseObservation();
        byte[] after = await topology.WaitForObserverAsync("after", token).ConfigureAwait(true);
        topology.Request["runEndedUtc"] = DateTimeOffset.UtcNow;
        await topology.RecheckBindingAsync(token).ConfigureAwait(true);
        PolicyRefusalNativeObservationVerifier.Verify(JsonSerializer.SerializeToElement(topology.Request), before, after, witness, observer);
        await topology.WriteAsync("producer-result.json", new
        {
            topology.RunId, classification = "bounded-missing-proof-preflight-only-unaccepted",
            bothGenericHttpDependencyResponsesObserved = true, independentlyVerifiedPolicyGateCauses = true,
            independentlySignedUnchangedDatabase = true,
            ac5PhysicalIsolationAccepted = false, staleProofQualified = false, outageQualified = false,
            a6Accepted = false, te2Accepted = false, storyComplete = false,
            beforeEnvelopeSha256 = PolicyRefusalNativeObservationVerifier.Hash(before),
            afterEnvelopeSha256 = PolicyRefusalNativeObservationVerifier.Hash(after),
        }, token).ConfigureAwait(true);
        output.WriteLine("Bounded missing-proof preflight verified. AC5, A6, stale-proof/outage and TE-2 remain open.");
    }

    /// <summary>Operational witness enrollment is separate from runtime opt-in and never inferred from producer output.</summary>
    [Fact]
    public void NativeVerifierMustRequireExternalEvidenceInsteadOfAnOptInFlag()
    {
        using RSA witness = RSA.Create(3072);
        using JsonDocument empty = JsonDocument.Parse("{}");
        Should.Throw<InvalidOperationException>(() => PolicyRefusalNativeObservationVerifier.Verify(empty.RootElement,
            "{}"u8.ToArray(), "{}"u8.ToArray(), witness, "independent-observer"));
    }

    /// <summary>Generic HTTP dependency responses cannot bypass the missing exact ChatBot-owned cause observation.</summary>
    [Fact]
    public void CurrentCandidateMustRemainBlockedWithoutChatBotOwnedCauseReader()
    {
        Should.Throw<InvalidOperationException>(PolicyRefusalPreflightTopology.RequirePolicyGateCauseObservation)
            .Message.ShouldContain("InMemoryAuditWriter.AuthorizationFailures has no supported cross-process reader");
    }
}
