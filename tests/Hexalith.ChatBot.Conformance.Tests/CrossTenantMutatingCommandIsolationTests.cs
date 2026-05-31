using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// AC2 / AC6 — mutating command paths fail closed before durable work, for every one of the nine personas, when
/// a principal bound to <c>tenant-alpha</c> submits a command targeting <c>tenant-beta</c> or carries
/// stale/missing/ambiguous/unsafe tenant context. Every case runs through the REAL gateway lane and asserts the
/// gateway captures/stores, never just a status code: the denial is metadata-only, the single authorization
/// failure fact is recorded, and there is zero durable work (no dispatch, no coarse-idempotency admission, no
/// pre/post-commit audit envelope, no operation-status record, and — since nothing dispatched — no governed
/// projection). Each serialized artifact is routed through the shared leakage gate.
/// </summary>
public sealed class CrossTenantMutatingCommandIsolationTests
{
    [Fact]
    public async Task ForeignTenantInCommandBodyShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.ForeignTargetCommandBody,
            ChatBotAuthorizationReasonCodes.TenantMismatch);

    [Fact]
    public async Task ForeignTenantInScopedIdentifierShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.ForeignTargetScopedIdentifier,
            ChatBotAuthorizationReasonCodes.TenantMismatch);

    [Fact]
    public async Task ForeignTenantNestedInJsonBodyShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.ForeignTargetNestedJson,
            ChatBotAuthorizationReasonCodes.TenantMismatch);

    [Fact]
    public async Task MissingTenantClaimShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.MissingTenantClaim,
            ChatBotAuthorizationReasonCodes.TenantMissing);

    [Fact]
    public async Task AmbiguousMultipleTenantClaimsShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.MultipleTenantClaims,
            ChatBotAuthorizationReasonCodes.TenantMissing);

    [Fact]
    public async Task StaleTenantClaimShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.StaleTenantClaim,
            ChatBotAuthorizationReasonCodes.TenantMismatch);

    [Fact]
    public async Task UnsafeTenantClaimShouldFailClosedForEveryPersona()
        => await AssertEveryPersonaFailsClosedAsync(
            CrossTenantIsolationHarness.TenantContextVariant.UnsafeTenantClaim,
            ChatBotAuthorizationReasonCodes.TenantMissing);

    private static async Task AssertEveryPersonaFailsClosedAsync(
        CrossTenantIsolationHarness.TenantContextVariant variant,
        string expectedReasonCode)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        foreach (IsolationActorPersona persona in IsolationActorMatrix.Personas)
        {
            CrossTenantIsolationHarness.MutatingDenialOutcome outcome = await CrossTenantIsolationHarness
                .RunMutatingDenialAsync(persona, variant, token)
                .ConfigureAwait(false);

            // First-class denial, compared on the catalog-backed problem and the failure fact — never a bare code.
            outcome.IsAccepted.ShouldBeFalse();
            outcome.ProblemCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
            outcome.AuthorizationFailureCount.ShouldBe(1);
            outcome.AuthorizationFailureReasonCode.ShouldBe(expectedReasonCode);

            // The declared surface origin is attributed on the denial fact (the single permitted delta).
            outcome.AuthorizationFailureSurfaceOrigin.ShouldBe(persona.DeclaredOrigin);

            // Fail-closed BEFORE any durable-state work.
            outcome.DispatchCount.ShouldBe(0);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(0);
            outcome.AuditEnvelopeCount.ShouldBe(0);
            outcome.OperationStatusRecordExists.ShouldBeFalse();

            // Leakage gate: the user-facing problem must contain ZERO sentinels (full corpus). The internal
            // failure fact legitimately records the bound tenant as the read scope, so it is scanned excluding
            // ONLY that token — it must still never carry the foreign tenant or any candidate/evidence/file/
            // cursor/path/exception payload pulled from the command.
            CrossTenantLeakageScanner.Scan(persona.Label, "command-problem", outcome.SerializedProblem, CrossTenantLeakageCorpus.Sentinels);
            CrossTenantLeakageScanner.Scan(
                persona.Label,
                "authorization-failure-fact",
                outcome.SerializedAuthorizationFailures,
                CrossTenantLeakageCorpus.SentinelsExcluding(CrossTenantLeakageCorpus.BoundTenant));
        }
    }
}
