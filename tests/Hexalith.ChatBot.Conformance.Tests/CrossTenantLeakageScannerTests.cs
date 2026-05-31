using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// AC4 / AC7 — the leakage corpus + scanner. The corpus must cover every required channel; the scanner must
/// detect and name each sentinel class in a leaking artifact while never dumping the body; and — critically — it
/// must refuse to run with no sentinels so a forgotten scan cannot vacuously pass.
/// </summary>
public sealed class CrossTenantLeakageScannerTests
{
    [Fact]
    public void CorpusShouldCoverEveryRequiredChannelWithANonEmptySentinel()
    {
        CrossTenantLeakageCorpus.Sentinels.ShouldNotBeEmpty();
        CrossTenantLeakageCorpus.RequiredChannels.ShouldNotBeEmpty();
        CrossTenantLeakageCorpus.Sentinels.ShouldAllBe(static sentinel =>
            !string.IsNullOrWhiteSpace(sentinel.Channel) && !string.IsNullOrWhiteSpace(sentinel.Value));

        foreach (string channel in CrossTenantLeakageCorpus.RequiredChannels)
        {
            CrossTenantLeakageCorpus.Sentinels
                .Any(sentinel => string.Equals(sentinel.Channel, channel, StringComparison.Ordinal))
                .ShouldBeTrue($"required channel '{channel}' has no sentinel");
        }
    }

    [Fact]
    public void ScannerShouldDetectAndNameEachSentinelClassWithoutDumpingTheBody()
    {
        foreach (LeakageSentinel sentinel in CrossTenantLeakageCorpus.Sentinels)
        {
            const string secretContext = "outer-body-context-do-not-dump";
            string leakingArtifact = $"{{\"context\":\"{secretContext}\",\"detail\":\"{sentinel.Value}\"}}";

            // Scan with the single sentinel so the matched class is deterministic (some sentinel values are
            // substrings of others, e.g. the provider-snippet contains the foreign tenant token).
            CrossTenantLeakageException leak = Should.Throw<CrossTenantLeakageException>(() =>
                CrossTenantLeakageScanner.Scan("meta-persona", "deliberately-leaking", leakingArtifact, [sentinel]));

            leak.Persona.ShouldBe("meta-persona");
            leak.ChannelLabel.ShouldBe("deliberately-leaking");
            leak.SentinelChannel.ShouldBe(sentinel.Channel);
            leak.SentinelValue.ShouldBe(sentinel.Value);
            // The diagnostic names the channel/sentinel but never dumps the offending body.
            leak.Message.ShouldContain(sentinel.Channel);
            leak.Message.ShouldNotContain(secretContext);
        }
    }

    [Fact]
    public void ScanAllShouldFailOnAFullyLeakingArtifact()
    {
        string leaking = $"{{\"a\":\"{CrossTenantLeakageCorpus.ForeignTenant}\",\"b\":\"{CrossTenantLeakageCorpus.Sentinel("candidate")}\"}}";

        Should.Throw<CrossTenantLeakageException>(() =>
            CrossTenantLeakageScanner.ScanAll("meta-persona", "fully-leaking", leaking));
    }

    [Fact]
    public void ScannerShouldThrowWhenInvokedWithNoSentinelsSoAForgottenScanCannotVacuouslyPass()
    {
        // Vacuity guard: a scan with an empty sentinel set would pass on ANY body, including a fully leaking one.
        Should.Throw<InvalidOperationException>(() =>
            CrossTenantLeakageScanner.Scan(
                "human-user",
                "unscanned-artifact",
                $"this body fully leaks {CrossTenantLeakageCorpus.ForeignTenant}",
                []));
    }

    [Fact]
    public void ScannerShouldPassACleanMetadataOnlyArtifact()
    {
        const string cleanProblem = "{\"code\":\"authorization_denied\",\"correlationId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAW\"}";

        Should.NotThrow(() => CrossTenantLeakageScanner.ScanAll("human-user", "clean-problem", cleanProblem));
    }
}
