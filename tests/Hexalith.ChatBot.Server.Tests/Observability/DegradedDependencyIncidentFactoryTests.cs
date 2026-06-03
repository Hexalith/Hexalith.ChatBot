using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 8.5 AC2: the incident factory emits exactly one valid metadata-only incident for a degraded/failed
/// dependency (narrowest scope, fixed 300s budget, deterministic owner-role/next-action) and returns
/// <see langword="null"/> for a healthy/unknown signal — never fabricating a degraded incident from healthy data.
/// </summary>
public sealed class DegradedDependencyIncidentFactoryTests
{
    private static readonly DateTimeOffset DetectedAt = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ChatBotHealthStatus.Degraded)]
    [InlineData(ChatBotHealthStatus.Failed)]
    public void FiresOneValidIncidentForDegradedOrFailed(ChatBotHealthStatus health)
    {
        DegradedDependencyIncident? incident = DegradedDependencyIncidentFactory.Create(
            "graph-subscription",
            health,
            new ScopeCandidates(MailboxRef: "mb-01", TenantRef: "t-alpha"),
            ChatBotMessageCodes.DegradedMailbox,
            ownerRole: null,
            nextSafeAction: null,
            "correlation-alpha",
            DetectedAt);

        incident.ShouldNotBeNull();
        incident.Health.ShouldBe(health);
        incident.DetectionBudgetSeconds.ShouldBe(DegradedDependencyContractValidator.DefaultDetectionBudgetSeconds);
        incident.DetectionBudgetSeconds.ShouldBe(300);
        // Narrowest present scope is mailbox (tenant is broader and ignored).
        incident.ScopeKind.ShouldBe(DependencyScopeKind.Mailbox);
        incident.AffectedScope.ShouldBe("mailbox:mb-01");
        // Deterministic owner-role derivation from the reason code; default next safe action.
        incident.OwnerRole.ShouldBe("mailbox-admin");
        incident.NextSafeAction.ShouldBe("escalate-to-operations");
        incident.ReasonCode.ShouldBe(ChatBotMessageCodes.DegradedMailbox);
        DegradedDependencyContractValidator.IsValid(incident).ShouldBeTrue();
    }

    [Theory]
    [InlineData(ChatBotHealthStatus.Healthy)]
    [InlineData(ChatBotHealthStatus.Unknown)]
    public void ReturnsNullForHealthyOrUnknownNeverFabricatingAnIncident(ChatBotHealthStatus health)
        => DegradedDependencyIncidentFactory.Create(
            "graph-subscription",
            health,
            new ScopeCandidates(MailboxRef: "mb-01"),
            ChatBotMessageCodes.DependencyDegraded,
            ownerRole: null,
            nextSafeAction: null,
            "correlation-alpha",
            DetectedAt).ShouldBeNull();

    [Fact]
    public void NormalizesDetectionInstantToUtc()
    {
        DateTimeOffset offsetDetected = new(2026, 6, 3, 6, 0, 0, TimeSpan.FromHours(2));
        DegradedDependencyIncident? incident = DegradedDependencyIncidentFactory.Create(
            "graph-subscription",
            ChatBotHealthStatus.Failed,
            new ScopeCandidates(WorkflowItemRef: "wi-1"),
            ChatBotMessageCodes.TerminalFailure,
            ownerRole: null,
            nextSafeAction: null,
            "correlation-alpha",
            offsetDetected);

        incident.ShouldNotBeNull();
        incident.DetectedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        incident.DetectedAtUtc.ShouldBe(offsetDetected.ToUniversalTime());
        // Workflow-item is the narrowest scope and wins over nothing-else.
        incident.ScopeKind.ShouldBe(DependencyScopeKind.WorkflowItem);
        incident.AffectedScope.ShouldBe("workflow-item:wi-1");
    }

    [Fact]
    public void ExplicitOwnerRoleAndNextActionOverrideTheDeterministicDefaults()
    {
        DegradedDependencyIncident? incident = DegradedDependencyIncidentFactory.Create(
            "audit-projection",
            ChatBotHealthStatus.Degraded,
            new ScopeCandidates(TenantRef: "t-alpha"),
            ChatBotMessageCodes.AuditUnavailable,
            ownerRole: "operations-admin",
            nextSafeAction: "review-failed-queue",
            "correlation-alpha",
            DetectedAt);

        incident.ShouldNotBeNull();
        incident.OwnerRole.ShouldBe("operations-admin");
        incident.NextSafeAction.ShouldBe("review-failed-queue");
        incident.ScopeKind.ShouldBe(DependencyScopeKind.Tenant);
        DegradedDependencyContractValidator.IsValid(incident).ShouldBeTrue();
    }

    [Fact]
    public void DefaultsOwnerRoleToOperationsAdminForAnUnmappedReason()
    {
        DegradedDependencyIncident? incident = DegradedDependencyIncidentFactory.Create(
            "dispatch",
            ChatBotHealthStatus.Failed,
            new ScopeCandidates(ProjectRef: "project:alpha"),
            ChatBotMessageCodes.FailedCommand,
            ownerRole: null,
            nextSafeAction: null,
            "correlation-alpha",
            DetectedAt);

        incident.ShouldNotBeNull();
        incident.OwnerRole.ShouldBe("operations-admin");
    }
}
