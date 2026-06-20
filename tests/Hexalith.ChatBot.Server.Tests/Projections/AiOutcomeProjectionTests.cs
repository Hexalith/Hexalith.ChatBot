using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class AiOutcomeProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset OccurredAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldMaterializeMetadataOnlyGovernedAiOutcomeRowWithTenantIsolation()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        AiOutcomeProjectionHandler.ProjectionOutcome outcome =
            await handler.HandleAsync(Published(10), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        ProjectConversationPage foreign = await store.ReadPageAsync(OtherTenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AiOutcomeProjectionHandler.ProjectionOutcome.Applied);
        item.Kind.ShouldBe(ProjectConversationItemKind.AiOutcome);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.AiActor);
        item.ItemId.ShouldBe(ProjectConversationItemView.AiOutcomeItemIdFor("proposal-001", AiOutcomeKind.Proposal, 10));
        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.Proposal);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Proposed);
        item.AiActorId.ShouldBe("ai-actor-001");
        item.AiActorType.ShouldBe("ai");
        item.AiProposalId.ShouldBe("proposal-001");
        item.AiSafeNextAction.ShouldBe("review-ai-action");
        item.RedactionState.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        foreign.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldExposeServerVerifiedAiResponseProgressFromMetadataOnlyOutcome()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(
            Published(15) with
            {
                AiResponseSequence = 7,
                AiResponseProgressState = "rendering",
                AiResponseTerminalReason = "none",
                AiResponseVisibilityState = "metadata_only",
                AiResponseIsTerminal = false,
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        AiResponseProgress progress = item.BuildAiResponseProgress().ShouldNotBeNull();

        progress.ProjectId.ShouldBe("project-001");
        progress.ResponseId.ShouldBe("proposal-001");
        progress.GenerationId.ShouldBe("operation-001");
        progress.Sequence.ShouldBe(7);
        progress.State.ShouldBe(AiResponseProgressState.Rendering);
        progress.IsTerminal.ShouldBeFalse();
        progress.RedactionState.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
    }

    [Fact]
    public async Task ShouldKeepProposalDenialAndOutcomeAsDistinctAppendOnlyHistoryRows()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(Published(10), TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            Published(11) with { OutcomeKind = AiOutcomeKind.Denial, OutcomeStatus = AiOutcomeStatus.Denied },
            TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            Published(12) with { OutcomeKind = AiOutcomeKind.OutcomeRecorded, OutcomeStatus = AiOutcomeStatus.Succeeded },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .OrderBy(static item => item.SourceVersion)
            .ToArray();

        items.Select(static item => item.AiOutcomeKind)
            .ShouldBe([AiOutcomeKind.Proposal, AiOutcomeKind.Denial, AiOutcomeKind.OutcomeRecorded], ignoreOrder: false);
        items.Select(static item => item.ItemId).Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public async Task ShouldExposeLifecycleReviewHistoryWithProposalOperationAndCorrelationIdentifiers()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(
            Published(13) with { OutcomeKind = AiOutcomeKind.ExecutionStarted, OutcomeStatus = AiOutcomeStatus.Executing },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        ProjectConversationReviewHistoryEntry entry = item.BuildReviewHistory().ShouldHaveSingleItem();

        entry.ReviewedResourceKind.ShouldBe("ai-outcome");
        entry.ReviewedResourceId.ShouldBe("proposal-001");
        entry.ActionCode.ShouldBe("execution-started");
        entry.DecisionCode.ShouldBe("executing");
        entry.OperationId.ShouldBe("operation-001");
        entry.CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public async Task ShouldRenderSafeMetadataWhenExecutionResultArrivesBeforeProposal()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        // Execution-failed arrives first (out-of-order), proposal later — both render append-only.
        await handler.HandleAsync(
            Published(14) with { OutcomeKind = AiOutcomeKind.ExecutionFailed, OutcomeStatus = AiOutcomeStatus.Failed, FailureCode = "tool-timeout" },
            TestContext.Current.CancellationToken);
        await handler.HandleAsync(Published(10), TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ToArray();

        items.Length.ShouldBe(2);
        items.ShouldContain(static item => item.AiOutcomeKind == AiOutcomeKind.ExecutionFailed && item.AiFailureCode == "tool-timeout");
        items.ShouldContain(static item => item.AiOutcomeKind == AiOutcomeKind.Proposal);
    }

    [Theory]
    [InlineData(AiOutcomeKind.Proposal, AiOutcomeStatus.Proposed)]
    [InlineData(AiOutcomeKind.Denial, AiOutcomeStatus.Denied)]
    [InlineData(AiOutcomeKind.Refusal, AiOutcomeStatus.Blocked)]
    [InlineData(AiOutcomeKind.ApprovalLinked, AiOutcomeStatus.PendingApproval)]
    [InlineData(AiOutcomeKind.ExecutionStarted, AiOutcomeStatus.Executing)]
    [InlineData(AiOutcomeKind.ExecutionSucceeded, AiOutcomeStatus.Succeeded)]
    [InlineData(AiOutcomeKind.ExecutionFailed, AiOutcomeStatus.Failed)]
    [InlineData(AiOutcomeKind.OutcomeRecorded, AiOutcomeStatus.Succeeded)]
    [InlineData(AiOutcomeKind.CorrectedContextInvalidated, AiOutcomeStatus.Invalidated)]
    public async Task ShouldMaterializeEveryGovernedAiOutcomeKind(AiOutcomeKind kind, AiOutcomeStatus status)
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(
            Published((long)kind + 100) with { OutcomeKind = kind, OutcomeStatus = status },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(kind);
        item.AiOutcomeStatus.ShouldBe(status);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.AiActor);
        item.RedactionState.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
    }

    [Fact]
    public async Task LowRiskExecutionEventsShouldProjectAsAppendOnlyAiOutcomeRows()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);
        LowRiskAiAssistanceExecutionStarted started = new(
            "ai-execution-001",
            "proposal-001",
            "project-001",
            "task-intent-001",
            "graph-message-001",
            "requester-001",
            "summarize-visible-context",
            "context-package-001",
            "v1",
            "policy-snap-001",
            "low-risk-execute-allowed",
            8,
            CorrelationId,
            OccurredAt);
        LowRiskAiAssistanceExecutionRecord record = new(
            "ai-execution-001",
            "proposal-001",
            "summarize-visible-context",
            "success",
            "deterministic-test",
            "test-model-v1",
            OccurredAt.AddSeconds(5),
            ["evidence-001"],
            "context-package-001",
            "v1",
            "metadata_only",
            "policy-snap-001",
            "low-risk-execute-allowed",
            "audit:ai-execution-001",
            "available",
            CorrelationId,
            "metadata_only",
            "metadata_only",
            "none");

        await handler.HandleAsync(LowRiskAiOutcomeProjectionTranslator.FromStarted(Tenant, "ai-actor-001", 70, started), TestContext.Current.CancellationToken);
        await handler.HandleAsync(LowRiskAiOutcomeProjectionTranslator.FromCompleted(Tenant, "project-001", "ai-actor-001", 71, record), TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .OrderBy(static item => item.SourceVersion)
            .ToArray();

        items.Select(static item => item.AiOutcomeKind).ShouldBe(
            [AiOutcomeKind.ExecutionStarted, AiOutcomeKind.ExecutionSucceeded],
            ignoreOrder: false);
        items[1].AiPolicyReasonCode.ShouldBe("low-risk-execute-allowed");
        items[1].AiRiskClass.ShouldBe(AiActionRiskClass.LowRisk);
        items[1].AiContextPackageId.ShouldBe("context-package-001");
        items[1].AiAuthorizedContextReferences.ShouldBe(["evidence-001"]);
        items[1].AiSafeNextAction.ShouldBe("none");
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyLowRiskAiAssistanceSuccessDomainEvent()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        LowRiskAiAssistanceExecutionRecord record = new(
            "ai-execution-001",
            "proposal-001",
            "summarize-visible-context",
            "success",
            "deterministic-test",
            "test-model-v1",
            OccurredAt.AddSeconds(5),
            ["evidence-001"],
            "context-package-001",
            "v1",
            "metadata_only",
            "policy-snap-001",
            "low-risk-execute-allowed",
            "audit:ai-execution-001",
            "available",
            CorrelationId,
            "metadata_only",
            "metadata_only",
            "none");
        PublishedAiActionExecutionEvent published = new(
            Tenant,
            ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain,
            "graph-message-001",
            typeof(LowRiskAiAssistanceExecutionSucceeded).FullName,
            71,
            OccurredAt.AddSeconds(5),
            CorrelationId,
            LowRiskSucceeded: new LowRiskAiAssistanceExecutionSucceeded(
                record,
                "project-001",
                "requester-001",
                "graph-message-001",
                "conversation-item-001",
                ["evidence-001"],
                ["redacted", "policy-denied"]));

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        IProjectConversationProjectionStore store = factory.Services.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.ExecutionSucceeded);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Succeeded);
        item.AiPolicyReasonCode.ShouldBe("low-risk-execute-allowed");
        item.AiRiskClass.ShouldBe(AiActionRiskClass.LowRisk);
        item.AiContextPackageId.ShouldBe("context-package-001");
        item.AiAuthorizedContextReferences.ShouldBe(["evidence-001"]);
        item.AiExcludedContextReasons.ShouldBe(["redacted", "policy-denied"], ignoreOrder: true);
        item.AiRequesterId.ShouldBe("requester-001");
        item.AiSourceMessageId.ShouldBe("graph-message-001");
        item.AiSafeNextAction.ShouldBe("none");
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyLowRiskAiAssistanceRoutedToApprovalDomainEvent()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        LowRiskAiAssistanceExecutionRecord record = new(
            "ai-execution-002",
            "proposal-002",
            "summarize-visible-context",
            "pending-approval",
            "not-invoked",
            "not-invoked",
            OccurredAt.AddSeconds(5),
            ["evidence-001"],
            "context-package-001",
            "v1",
            "metadata_only",
            "policy-snap-001",
            "low-risk-policy-false",
            "audit:ai-execution-002",
            "available",
            CorrelationId,
            "metadata_only",
            "metadata_only",
            "review-ai-action",
            FailureCode: "low-risk-policy-false");
        PublishedAiActionExecutionEvent published = new(
            Tenant,
            ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain,
            "graph-message-002",
            typeof(LowRiskAiAssistanceRoutedToApproval).FullName,
            72,
            OccurredAt.AddSeconds(5),
            CorrelationId,
            LowRiskRoutedToApproval: new LowRiskAiAssistanceRoutedToApproval(
                record,
                "project-001",
                "requester-001",
                "graph-message-002",
                "conversation-item-002",
                ["evidence-001"],
                ["redacted"]));

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        IProjectConversationProjectionStore store = factory.Services.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.ApprovalLinked);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.PendingApproval);
        item.AiSafeNextAction.ShouldBe("review-ai-action");
        item.AiExcludedContextReasons.ShouldBe(["redacted"]);
    }

    [Fact]
    public async Task ApprovedCommandExecutionShouldProjectStartedSucceededAndOutcomeRecordedRows()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);
        ApprovedAiActionExecutionStarted started = new(
            "ai-approved-execution-001",
            "proposal-001",
            "approval:proposal-001",
            "project-001",
            "task-intent-001",
            "graph-message-001",
            "conversation-item-001",
            "requester-001",
            "Project.AppendConversationMessage",
            "ai-action-command-allowlist.m0",
            10,
            9,
            "policy-snap-001",
            CorrelationId,
            OccurredAt);
        ApprovedAiActionExecutionRecord record = new(
            "ai-approved-execution-001",
            "proposal-001",
            "approval:proposal-001",
            "Project.AppendConversationMessage",
            "ai-action-command-allowlist.m0",
            "success",
            OccurredAt.AddSeconds(5),
            "audit:ai-approved-execution-001",
            "available",
            CorrelationId,
            "metadata_only",
            "none");

        await handler.HandleAsync(ApprovedAiActionOutcomeProjectionTranslator.FromStarted(Tenant, "ai-actor-001", 80, started), TestContext.Current.CancellationToken);
        await handler.HandleAsync(ApprovedAiActionOutcomeProjectionTranslator.FromCompleted(Tenant, "project-001", "ai-actor-001", 81, record), TestContext.Current.CancellationToken);
        await handler.HandleAsync(ApprovedAiActionOutcomeProjectionTranslator.FromOutcomeRecorded(Tenant, "project-001", "ai-actor-001", 82, record), TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .OrderBy(static item => item.SourceVersion)
            .ToArray();

        items.Select(static item => item.AiOutcomeKind).ShouldBe(
            [AiOutcomeKind.ExecutionStarted, AiOutcomeKind.ExecutionSucceeded, AiOutcomeKind.OutcomeRecorded],
            ignoreOrder: false);
        items[0].AiExecutionStatus.ShouldBe("executing");
        items[0].AiSafeNextAction.ShouldBe("wait-for-command-outcome");
        items[1].AiCommandName.ShouldBe("Project.AppendConversationMessage");
        items[1].AiCommandAllowlistVersion.ShouldBe("ai-action-command-allowlist.m0");
        items[1].AiApprovalId.ShouldBe("approval:proposal-001");
        items[1].AiExecutionStatus.ShouldBe("success");
        items[1].AiExecutionOutcomeCode.ShouldBe("approved-ai-action-executed");
        items[1].AiGeneratedContentVisibility.ShouldBe("metadata_only");
        items[2].AiExecutionOutcomeCode.ShouldBe("outcome-recorded");
    }

    [Fact]
    public async Task ApprovedCommandExecutionRejectionShouldProjectBlockedRefusalRow()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);
        ApprovedAiActionExecutionRejected rejected = new(
            "ai-approved-execution-001",
            "proposal-001",
            "approval:proposal-001",
            "project-001",
            "task-intent-001",
            "graph-message-001",
            "conversation-item-001",
            "requester-001",
            "Project.AppendConversationMessage",
            "ai-action-command-allowlist.m0",
            ChatBotRefusalReasonCodes.EvidenceExpired,
            10,
            CorrelationId,
            "policy-snap-001");

        await handler.HandleAsync(
            ApprovedAiActionOutcomeProjectionTranslator.FromRejected(Tenant, "ai-actor-001", 83, OccurredAt, rejected),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.Refusal);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Blocked);
        item.AiFailureCode.ShouldBe(ChatBotRefusalReasonCodes.EvidenceExpired);
        item.AiExecutionOutcomeCode.ShouldBe(ChatBotRefusalReasonCodes.EvidenceExpired);
        item.AiSafeNextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        item.AiPolicySnapshotId.ShouldBe("policy-snap-001");
        item.AiRequesterId.ShouldBe("requester-001");
        item.AiSourceMessageId.ShouldBe("graph-message-001");
        item.RedactionState.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
    }

    [Fact]
    public async Task ProposalInvalidationShouldProjectCorrectedContextInvalidatedMetadataOnlyRow()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);
        AiActionProposalInvalidatedByCorrection invalidated = new(
            "proposal-001",
            "approval:proposal-001",
            "task-intent-001",
            "graph-message-001",
            "conversation-item-001",
            "requester-001",
            "project-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
            "corrected",
            11,
            CorrelationId,
            ChatBotDetailVisibility.MetadataOnly,
            "collaboration_input");

        await handler.HandleAsync(
            ApprovedAiActionOutcomeProjectionTranslator.FromInvalidated(Tenant, "ai-action-invalidator", 84, OccurredAt, invalidated),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.CorrectedContextInvalidated);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Invalidated);
        item.AiFailureCode.ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
        item.AiExecutionOutcomeCode.ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
        item.AiApprovalId.ShouldBe("approval:proposal-001");
        IReadOnlyList<string> contextReferences = item.AiAuthorizedContextReferences.ShouldNotBeNull();
        contextReferences.ShouldContain("association:01ARZ3NDEKTSV4RRFFQ69G5FAV");
        contextReferences.ShouldContain("correction:01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11");
        contextReferences.ShouldContain("evidence-state:corrected");
        item.RedactionState.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
    }

    [Fact]
    public async Task LowRiskPolicyFalseShouldProjectAsApprovalLinkedPendingRow()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);
        LowRiskAiAssistanceExecutionRecord record = new(
            "ai-execution-001",
            "proposal-001",
            "summarize-visible-context",
            "pending-approval",
            "not-invoked",
            "not-invoked",
            OccurredAt,
            ["evidence-001"],
            "context-package-001",
            "v1",
            "metadata_only",
            "policy-snap-001",
            "low_risk_policy_false",
            "audit:ai-execution-001",
            "available",
            CorrelationId,
            "metadata_only",
            "metadata_only",
            "review-ai-action",
            FailureCode: "low_risk_policy_false");

        await handler.HandleAsync(LowRiskAiOutcomeProjectionTranslator.FromCompleted(Tenant, "project-001", "ai-actor-001", 72, record), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.ApprovalLinked);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.PendingApproval);
        item.AiApprovalStatus.ShouldBe("pending");
        item.AiPolicyReasonCode.ShouldBe("low_risk_policy_false");
        item.AiSafeNextAction.ShouldBe("review-ai-action");
    }

    [Fact]
    public async Task DuplicateDeliveryShouldBeIdempotentAndStaleReplayShouldNotOverwriteNewerState()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(
            Published(20) with { OutcomeStatus = AiOutcomeStatus.Approved },
            TestContext.Current.CancellationToken);
        // Duplicate same source version is idempotent (deterministic same-version replacement).
        await handler.HandleAsync(
            Published(20) with { OutcomeStatus = AiOutcomeStatus.Approved },
            TestContext.Current.CancellationToken);
        // Stale replay is an older append-only row; it must not mutate the newer row's state.
        await handler.HandleAsync(
            Published(19) with { OutcomeStatus = AiOutcomeStatus.Proposed },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .OrderBy(static item => item.SourceVersion)
            .ToArray();

        // Append-only: the older (19) and newer (20) versions are distinct rows; the duplicate did not add a third.
        items.Length.ShouldBe(2);
        items.Count(static item => item.SourceVersion == 20).ShouldBe(1);
        items.Single(static item => item.SourceVersion == 20).AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Approved);
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyPublishedAiOutcomeAndStayIdempotentOnReplay()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                Published(60),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                Published(60),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);

        IProjectConversationProjectionStore store = factory.Services.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationPage page = await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);
        ProjectConversationItemView item = page.Items.ShouldHaveSingleItem();
        item.Kind.ShouldBe(ProjectConversationItemKind.AiOutcome);
        item.SourceVersion.ShouldBe(60);
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyApprovedAiActionExecutionDomainEvent()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        ApprovedAiActionExecutionRecord record = new(
            "ai-approved-execution-001",
            "proposal-001",
            "approval:proposal-001",
            "Project.AppendConversationMessage",
            "ai-action-command-allowlist.m0",
            "success",
            OccurredAt.AddSeconds(5),
            "audit:ai-approved-execution-001",
            "available",
            CorrelationId,
            "metadata_only",
            "none");
        PublishedAiActionExecutionEvent published = new(
            Tenant,
            ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain,
            "graph-message-001",
            typeof(ApprovedAiActionExecutionSucceeded).FullName,
            81,
            OccurredAt.AddSeconds(5),
            CorrelationId,
            Succeeded: new ApprovedAiActionExecutionSucceeded(
                record,
                "project-001",
                "requester-001",
                "graph-message-001",
                "conversation-item-001"));

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        IProjectConversationProjectionStore store = factory.Services.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .OrderBy(static item => item.SourceVersion)
            .ToArray();

        items.Select(static item => item.AiOutcomeKind).ShouldBe(
            [AiOutcomeKind.ExecutionSucceeded, AiOutcomeKind.OutcomeRecorded],
            ignoreOrder: false);
        items[0].AiRequesterId.ShouldBe("requester-001");
        items[0].AiSourceMessageId.ShouldBe("graph-message-001");
        items[0].AiExecutionOutcomeCode.ShouldBe("approved-ai-action-executed");
        items[1].AiExecutionOutcomeCode.ShouldBe("outcome-recorded");
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyApprovedAiActionExecutionRejectionDomainEvent()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        PublishedAiActionExecutionEvent published = new(
            Tenant,
            ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain,
            "graph-message-001",
            typeof(ApprovedAiActionExecutionRejected).FullName,
            83,
            OccurredAt.AddSeconds(5),
            CorrelationId,
            Rejected: new ApprovedAiActionExecutionRejected(
                "ai-approved-execution-001",
                "proposal-001",
                "approval:proposal-001",
                "project-001",
                "task-intent-001",
                "graph-message-001",
                "conversation-item-001",
                "requester-001",
                "Project.AppendConversationMessage",
                "ai-action-command-allowlist.m0",
                ChatBotRefusalReasonCodes.ApprovalStateInvalid,
                10,
                CorrelationId,
                "policy-snap-001"));

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        IProjectConversationProjectionStore store = factory.Services.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.Refusal);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Blocked);
        item.AiFailureCode.ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
        item.AiSafeNextAction.ShouldBe(ChatBotMessageNextActions.None);
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyAiActionProposalInvalidatedByCorrectionDomainEvent()
    {
        // AC6: a proactive correction invalidation must project an append-only corrected-context row through the
        // published-event wire envelope, not just via the translator. This exercises the AiOutcome subscriber endpoint
        // and the TryCreatePublishedEvents Invalidated branch end-to-end, matching the Rejected/Started wire coverage.
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        PublishedAiActionExecutionEvent published = new(
            Tenant,
            ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain,
            "graph-message-001",
            typeof(AiActionProposalInvalidatedByCorrection).FullName,
            84,
            OccurredAt.AddSeconds(5),
            CorrelationId,
            Invalidated: new AiActionProposalInvalidatedByCorrection(
                "proposal-001",
                "approval:proposal-001",
                "task-intent-001",
                "graph-message-001",
                "conversation-item-001",
                "requester-001",
                "project-001",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
                "corrected",
                11,
                CorrelationId,
                ChatBotDetailVisibility.MetadataOnly,
                "collaboration_input"));

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        IProjectConversationProjectionStore store = factory.Services.GetRequiredService<IProjectConversationProjectionStore>();
        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiOutcomeKind.ShouldBe(AiOutcomeKind.CorrectedContextInvalidated);
        item.AiOutcomeStatus.ShouldBe(AiOutcomeStatus.Invalidated);
        item.AiFailureCode.ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
        item.AiExecutionOutcomeCode.ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
        item.AiApprovalId.ShouldBe("approval:proposal-001");
        item.AiSafeNextAction.ShouldBe(ChatBotMessageNextActions.ReviewSourceEvidence);
        IReadOnlyList<string> contextReferences = item.AiAuthorizedContextReferences.ShouldNotBeNull();
        contextReferences.ShouldContain("association:01ARZ3NDEKTSV4RRFFQ69G5FAV");
        contextReferences.ShouldContain("correction:01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11");
    }

    [Fact]
    public async Task ShouldRedactPolicyAndAuditIdentifiersWhenVisibilityOrStatusDisallow()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(
            Published(30) with
            {
                PolicySnapshotId = "policy-snap-001",
                PolicySnapshotVisibility = "restricted",
                AuditOperationId = "audit-op-001",
                AuditStatus = "unavailable",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.AiPolicySnapshotId.ShouldBeNull();
        item.AiPolicySnapshotVisibility.ShouldBe("restricted");
        item.AiAuditOperationId.ShouldBeNull();
        item.AiAuditStatus.ShouldBe("unavailable");
    }

    [Fact]
    public async Task ShouldSuppressUnsafeOptionalTokensAndNeverLeakPromptOrProviderText()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        await handler.HandleAsync(
            Published(40) with
            {
                CommandName = "delete all /home/administrator/secret.txt",
                ExcludedContextReasons = ["raw prompt: ignore previous instructions", "context-restricted"],
                GeneratedContentVisibility = "summary contains raw model output",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        string serialized = JsonSerializer.Serialize(item);

        item.AiCommandName.ShouldBeNull();
        item.AiGeneratedContentVisibility.ShouldBeNull();
        item.AiExcludedContextReasons.ShouldNotBeNull();
        item.AiExcludedContextReasons.ShouldContain("context-restricted");
        item.AiExcludedContextReasons.ShouldNotContain(static reason => reason.Contains(' '));
        serialized.ShouldNotContain("raw prompt", Case.Insensitive);
        serialized.ShouldNotContain("raw model output", Case.Insensitive);
        serialized.ShouldNotContain("/home/administrator", Case.Insensitive);
    }

    [Theory]
    [InlineData("wrong-domain", "ai", true)]
    [InlineData("ai-outcomes", "provider", true)]
    [InlineData("ai-outcomes", "ai", false)]
    public async Task ShouldIgnoreEventsWithUnsafeDomainActorTypeOrMissingIdentity(string domain, string actorType, bool hasIdentity)
    {
        InMemoryProjectConversationProjectionStore store = new();
        AiOutcomeProjectionHandler handler = new(store);

        PublishedAiOutcomeEvent published = Published(50) with
        {
            Domain = domain,
            ActorType = actorType,
            ProposalId = hasIdentity ? "proposal-001" : null,
            OperationId = null,
            RequestId = null,
        };

        AiOutcomeProjectionHandler.ProjectionOutcome outcome =
            await handler.HandleAsync(published, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AiOutcomeProjectionHandler.ProjectionOutcome.Ignored);
        (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();
    }

    private static PublishedAiOutcomeEvent Published(long sourceVersion)
        => new(
            Tenant,
            AiOutcomeProjectionTranslator.AiOutcomeDomain,
            "ai-aggregate-001",
            sourceVersion,
            OccurredAt.AddMinutes(sourceVersion),
            CorrelationId,
            "project-001",
            AiOutcomeKind.Proposal,
            AiOutcomeStatus.Proposed,
            "ai-actor-001",
            "ai",
            ProposalId: "proposal-001",
            RequestId: "request-001",
            RequesterId: "requester-001",
            SourceConversationItemId: "decision:source:001",
            SourceMessageId: "graph-message-001",
            OperationId: "operation-001",
            RiskClass: AiActionRiskClass.ApprovalRequired,
            RiskActionClasses: ["invokes-tools", "modifies-state"],
            PolicySnapshotId: "policy-snap-001",
            PolicySnapshotVisibility: "authorized",
            ContextPackageId: "context-package-001",
            AuthorizedContextReferences: ["evidence-001", "evidence-002"],
            CommandName: "send-summary",
            CommandAllowlistVersion: "allowlist.v1",
            ApprovalId: "approval-001",
            ApprovalStatus: "approved",
            ExecutionStatus: "pending",
            AuditOperationId: "audit-op-001",
            AuditStatus: "available",
            SafeNextAction: "review-ai-action");
}
