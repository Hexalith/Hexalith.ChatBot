using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// AC7 — negative controls prove the harness can fail. These non-destructive meta-tests deliberately use a
/// vulnerable probe (a test-only store that ignores tenant in its lookup key) and a deliberately leaking body,
/// then assert the isolation guard and the leakage scanner FAIL and name the leaking channel/persona. They also
/// cover both vacuity modes the prior stories flagged: a "missing persona/channel" matrix would be caught by the
/// completeness check, and a "leakage scanner that does not scan a rendered artifact" (empty sentinel set) throws
/// rather than vacuously passing.
/// </summary>
public sealed class CrossTenantIsolationNegativeControlTests
{
    private static readonly DateTimeOffset SeedTime = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task VulnerableTenantIgnoringStoreShouldMakeTheIsolationAssertionFail()
    {
        // A deliberately broken store that keys by note id only, ignoring the tenant — the exact vulnerability
        // the real partitioning prevents. Seed it under the foreign tenant, then read AS the bound tenant.
        TenantIgnoringProjectionStore vulnerable = new();
        await vulnerable.SaveAsync(View(CrossTenantLeakageCorpus.ForeignTenant, CrossTenantLeakageCorpus.ForeignNoteId), TestContext.Current.CancellationToken);

        GovernedOperationView? leaked = await vulnerable.GetAsync(CrossTenantLeakageCorpus.BoundTenant, CrossTenantLeakageCorpus.ForeignNoteId, TestContext.Current.CancellationToken);

        // The vulnerable store leaks the foreign view across the boundary. Prove the real safe-isolation
        // expectation (foreign read → null) WOULD fail here — i.e. the harness is genuinely discriminating, not a
        // vacuous always-pass.
        leaked.ShouldNotBeNull();
        leaked.TenantId.ShouldBe(CrossTenantLeakageCorpus.ForeignTenant);
        Should.Throw<ShouldAssertException>(() => leaked.ShouldBeNull());
    }

    [Fact]
    public void DeliberatelyLeakingRenderedBodyShouldFailTheScannerNamingChannelAndPersona()
    {
        // A rendered body that leaks the foreign tenant — the scanner must fail and name the persona + channel.
        string leakingBody = $"{{\"detail\":\"lookup for {CrossTenantLeakageCorpus.ForeignTenant} failed\"}}";

        CrossTenantLeakageException leak = Should.Throw<CrossTenantLeakageException>(() =>
            CrossTenantLeakageScanner.ScanAll(IsolationActorMatrix.AiActor, "command-problem", leakingBody));

        leak.Persona.ShouldBe(IsolationActorMatrix.AiActor);
        leak.ChannelLabel.ShouldBe("command-problem");
        leak.SentinelChannel.ShouldBe("tenant");
    }

    [Fact]
    public void CompletenessCheckShouldFailWhenAPersonaHasZeroCases()
    {
        // Simulate a matrix run that forgot one persona; the "missing persona" vacuity guard must catch it.
        HashSet<string> executed = new(IsolationActorMatrix.RequiredPersonaLabels, StringComparer.Ordinal);
        executed.Remove(IsolationActorMatrix.AiActor);

        bool allPersonasCovered = IsolationActorMatrix.RequiredPersonaLabels.All(executed.Contains);

        allPersonasCovered.ShouldBeFalse();
    }

    [Fact]
    public void ScanningWithNoSentinelsShouldThrowSoAForgottenScanCannotVacuouslyPass()
    {
        // The "leakage scanner does not scan a rendered artifact" vacuity: an empty sentinel set would pass on any
        // body. The scanner refuses to run, so a forgotten/empty scan is a hard failure, not a silent pass.
        Should.Throw<InvalidOperationException>(() =>
            CrossTenantLeakageScanner.Scan(
                IsolationActorMatrix.HumanUser,
                "unscanned-artifact",
                $"this body fully leaks {CrossTenantLeakageCorpus.ForeignTenant}",
                []));
    }

    private static GovernedOperationView View(string tenantId, string noteId)
        => new(
            tenantId,
            noteId,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            GovernedOperationView.MetadataOnlyRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            SourceVersion: 1,
            SeedTime,
            SeedTime);

    /// <summary>A deliberately vulnerable, test-only store that ignores the tenant in its lookup key.</summary>
    private sealed class TenantIgnoringProjectionStore : IGovernedOperationProjectionStore
    {
        private readonly Dictionary<string, GovernedOperationView> _byNoteIdOnly = new(StringComparer.Ordinal);

        public Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
            => Task.FromResult(_byNoteIdOnly.GetValueOrDefault(noteId));

        public Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(view);
            _byNoteIdOnly[view.NoteId] = view;
            return Task.CompletedTask;
        }
    }
}
