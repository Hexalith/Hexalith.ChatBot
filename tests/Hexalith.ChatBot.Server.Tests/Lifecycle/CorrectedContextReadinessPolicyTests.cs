using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class CorrectedContextReadinessPolicyTests
{
    [Fact]
    public async Task PolicyShouldBlockCorrectedContextUntilAllRequiredStoresComplete()
    {
        InMemoryAssociationProjectionStore store = new();
        ProjectionCorrectedContextReadinessPolicy policy = new(store);
        AssociationCandidateView view = new(
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "mailbox-001",
            "conversation-001",
            null,
            "project-002",
            null,
            LifecycleState.Correcting,
            AssociationScoringOutcome.CandidatesGenerated,
            AssociationThresholdBand.Ambiguous,
            0.7,
            [],
            [],
            "policy-v1",
            AssociationCandidateView.CurrentSchemaVersion,
            AssociationCandidateView.MailboxSourceProvenance,
            "kernel-v1",
            "metadata_only",
            "collaboration_input",
            3,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 9, 1, 0, TimeSpan.Zero),
            DownstreamImpactStatus: "correcting",
            RequiredStoreKeys: ["association-routing", "evidence-snapshot"],
            CompletedStoreKeys: ["association-routing"],
            PropagationStatus: "correcting",
            IsCorrectedContextStale: true);
        await store.SaveAsync(view, TestContext.Current.CancellationToken);

        CorrectedContextReadiness blocked = await policy.EvaluateAsync(
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            3,
            TestContext.Current.CancellationToken);

        blocked.IsUsable.ShouldBeFalse();
        blocked.ReasonCode.ShouldBe("association_ai_context_blocked");
        blocked.PendingStoreKeys.ShouldBe(["evidence-snapshot"]);

        await store.SaveAsync(view with
        {
            LifecycleState = LifecycleState.Corrected,
            DownstreamImpactStatus = "complete",
            CompletedStoreKeys = ["association-routing", "evidence-snapshot"],
            PropagationStatus = "complete",
            IsCorrectedContextStale = false,
        }, TestContext.Current.CancellationToken);

        CorrectedContextReadiness ready = await policy.EvaluateAsync(
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            3,
            TestContext.Current.CancellationToken);
        ready.IsUsable.ShouldBeTrue();
        ready.PendingStoreKeys.ShouldBeEmpty();
    }
}
