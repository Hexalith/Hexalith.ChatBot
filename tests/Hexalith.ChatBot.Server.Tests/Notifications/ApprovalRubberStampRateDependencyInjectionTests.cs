using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

/// <summary>
/// Story 7.11 wiring guard for <c>AddChatBotCommandGateway</c>: the rubber-stamp-rate coordinator must resolve with
/// the shared audit writer and system clock used by the runtime recorded-only path.
/// </summary>
public sealed class ApprovalRubberStampRateDependencyInjectionTests
{
    [Fact]
    public void ApprovalRubberStampRateRuntimeSeamsResolveToSharedInMemoryDefaults()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IAuditWriter>().ShouldBeOfType<ChainedAuditWriter>();
        provider.GetRequiredService<ISystemClock>().ShouldBeOfType<SystemClock>();
        provider.GetRequiredService<ApprovalRubberStampRateCoordinator>().ShouldNotBeNull();
    }

    [Fact]
    public async Task RegisteredCoordinatorRecordsMetadataOnlyTuningRevisitEnvelope()
    {
        using ServiceProvider provider = BuildProvider();
        ApprovalRubberStampRateCoordinator coordinator = provider.GetRequiredService<ApprovalRubberStampRateCoordinator>();

        ApprovalRubberStampRateOutcome outcome = await coordinator.EvaluateAndRecordAsync(
            Mix(rubberStamp: 4, slow: 16, reviewer: "reviewer-a"),
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        outcome.Evaluated.ShouldBe(1);
        outcome.Triggered.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);

        AuditEnvelope envelope = provider.GetRequiredService<InMemoryAuditWriter>().Envelopes.ShouldHaveSingleItem();
        envelope.TenantId.ShouldBe("tenant-alpha");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:approval-tuning-revisit-triggered");
        envelope.SourceEvidenceRefs.ShouldContain("risk-class:approval-required");
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-count:4");
        envelope.SourceEvidenceRefs.ShouldContain("approval-total:20");
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-rate-permille:200");
        envelope.SourceEvidenceRefs.ShouldContain("rubber-stamp-latency-seconds:5");
        envelope.SourceEvidenceRefs.ShouldContain("fatigue-fraction-percent:15");
        envelope.SourceEvidenceRefs.ShouldContain("rolling-window-days:7");
        envelope.SourceEvidenceRefs.ShouldContain("reviewer-rubber-stamp:reviewer-a:4:20");
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains('@', StringComparison.Ordinal));
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains("secret", StringComparison.OrdinalIgnoreCase));
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains("project-", StringComparison.OrdinalIgnoreCase));
        envelope.SourceEvidenceRefs.ShouldNotContain(reference => reference.Contains("proposal-", StringComparison.OrdinalIgnoreCase));
    }

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        _ = services.AddChatBotCommandGateway();
        return services.BuildServiceProvider();
    }

    private static List<ApprovalDecisionSample> Mix(int rubberStamp, int slow, string? reviewer)
    {
        DateTimeOffset decidedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        List<ApprovalDecisionSample> decisions = [];
        for (int i = 0; i < rubberStamp; i++)
        {
            decisions.Add(new ApprovalDecisionSample(
                "tenant-alpha",
                reviewer,
                decidedAt.AddSeconds(-1),
                decidedAt,
                ApprovalDecisionKind.Approve,
                AiActionRiskClass.ApprovalRequired));
        }

        for (int i = 0; i < slow; i++)
        {
            decisions.Add(new ApprovalDecisionSample(
                "tenant-alpha",
                reviewer,
                decidedAt.AddSeconds(-60),
                decidedAt,
                ApprovalDecisionKind.Approve,
                AiActionRiskClass.ApprovalRequired));
        }

        return decisions;
    }
}
