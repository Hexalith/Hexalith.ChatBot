using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Direct coverage for the Story 8.4 (AC7) <see cref="AuditEnvelopeFactory.OperationalAlertFired"/> pre-commit,
/// metadata-only audit envelope: the fixed command/decision/state-transition tokens, the pre-commit phase and
/// metadata-only redaction stage, the bounded safe ref list, and the safe folding of the space-separated
/// <c>tenant:{ref} mailbox:{ref}</c> scope to a single colon-delimited audit ref token.
/// </summary>
public sealed class AuditEnvelopeFactoryOperationalAlertTests
{
    private static readonly DateTimeOffset FiredAt = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WritesMetadataOnlyPreCommitEnvelopeWithFixedTokens()
    {
        OperationalAlertPayload alert = new(
            OperatorAlertKind.AuditProjectionLagBreached,
            "tenant:tenant-alpha",
            "operations-admin",
            "review-audit-projection-lag",
            "audit_projection_lag_breached",
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            FiredAt);

        AuditEnvelope envelope = AuditEnvelopeFactory.OperationalAlertFired(alert, FiredAt);

        envelope.TenantId.ShouldBe("tenant-alpha");
        envelope.ActorType.ShouldBe("system");
        envelope.CommandName.ShouldBe("OperationalAlertFired");
        envelope.ResourceId.ShouldBe("audit-projection-lag-breached");
        envelope.Decision.ShouldBe("alert");
        envelope.ReasonCode.ShouldBe("audit_projection_lag_breached");
        envelope.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        envelope.StateTransition.ShouldBe("Open->Alerted");
        envelope.Outcome.ShouldBe("alerted");
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);

        // The bounded safe ref list carries only aggregate tokens, never restricted content.
        envelope.SourceEvidenceRefs.ShouldContain("correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:operational-alert-fired");
        envelope.SourceEvidenceRefs.ShouldContain("operational-alert-kind:audit-projection-lag-breached");
        envelope.SourceEvidenceRefs.ShouldContain("operational-alert-reason:audit_projection_lag_breached");
        envelope.SourceEvidenceRefs.ShouldContain("operational-alert-owner-role:operations-admin");
        envelope.SourceEvidenceRefs.ShouldContain("operational-alert-next-action:review-audit-projection-lag");
        envelope.SourceEvidenceRefs.ShouldContain("operational-alert-scope:tenant:tenant-alpha");
    }

    [Fact]
    public void FoldsSpaceSeparatedMailboxScopeToSafeSingleTokenRef()
    {
        OperationalAlertPayload alert = new(
            OperatorAlertKind.SubscriptionExpiryImminent,
            "tenant:tenant-alpha mailbox:mb-1",
            "mailbox-admin",
            "renew-graph-subscription",
            "subscription_expiry_threshold_exceeded",
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            FiredAt);

        AuditEnvelope envelope = AuditEnvelopeFactory.OperationalAlertFired(alert, FiredAt);

        // The single space separator is folded to '|' so every audit ref stays a single space-free safe token.
        envelope.SourceEvidenceRefs.ShouldContain("operational-alert-scope:tenant:tenant-alpha|mailbox:mb-1");
        envelope.SourceEvidenceRefs.ShouldAllBe(static r => !r.Contains(' ', StringComparison.Ordinal));
        envelope.ResourceId.ShouldBe("subscription-expiry-imminent");
    }

    [Fact]
    public void CarriesNoRestrictedContentInAnyRef()
    {
        OperationalAlertPayload alert = new(
            OperatorAlertKind.AuthorizationFailureSpike,
            "tenant:tenant-alpha",
            "tenant-admin",
            "investigate-authorization-failures",
            "authorization_failure_spike_detected",
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            FiredAt);

        AuditEnvelope envelope = AuditEnvelopeFactory.OperationalAlertFired(alert, FiredAt);

        // No actor/command/project/secret leakage in any ref (NFR2/NFR42); refs are bounded aggregate tokens only.
        foreach (string banned in new[] { "secret", "password", "bearer", "@", ".txt", ".json" })
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(r => !r.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }
}
