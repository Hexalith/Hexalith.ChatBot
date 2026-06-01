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
