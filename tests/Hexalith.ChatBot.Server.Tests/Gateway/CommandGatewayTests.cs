using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CommandGatewayTests
{
    private const string ActorId = "actor-alpha";
    private const string BoundTenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";

    [Fact]
    public async Task GatewayShouldExecuteAdmissionStagesBeforeDispatch()
    {
        List<string> stages = [];
        RecordingDispatcher dispatcher = new(stages);
        CommandGateway gateway = new(
            new RecordingAuthenticationStage(stages),
            new RecordingTenantBindingStage(stages),
            new RecordingAuthorizationStage(stages, ChatBotAuthorizationResult.Allowed()),
            new RecordingRiskClassifier(stages),
            new RecordingApprovalGate(stages),
            new RecordingIdempotencyStore(stages),
            new RecordingAuditWriter(stages),
            new RecordingReplayIntentQueue(),
            new RecordingOperatorAlertSink(),
            new InMemoryOperationStatusStore(),
            new FixedClock(),
            new RecordingLifecycleTransitionGuard(stages),
            dispatcher,
            DefaultProblemDetailsFactory(),
            new PermissiveSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        stages.ShouldBe(
            [
                "auth",
                "tenant-bind",
                "authorize",
                "risk-classify",
                "approval-gate",
                "coarse-idempotency",
                "lifecycle-validation",
                "pre-commit-audit",
                "dispatch",
                "post-commit-audit",
            ]);
    }

    [Fact]
    public async Task EquivalentDuplicateShouldReplayPriorOutcomeAndNeverDispatchAgain()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink,
            idempotencyStore: idempotencyStore);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource"));

        ChatBotGatewayResult first = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        ChatBotGatewayResult second = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        second.IsAccepted.ShouldBeTrue();
        second.Accepted.ShouldNotBeNull();
        second.Accepted.CommandId.ShouldBe(first.Accepted!.CommandId);
        second.Accepted.CorrelationId.ShouldBe(first.Accepted.CorrelationId);
        second.Accepted.TaskId.ShouldBe(first.Accepted.TaskId);
        second.Accepted.LifecycleState.ShouldBe(first.Accepted.LifecycleState);
        second.Accepted.AcceptedAt.ShouldBe(first.Accepted.AcceptedAt);
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
        auditWriter.Envelopes.Select(static envelope => envelope.StateTransition).ShouldBe(
            ["Received->Proposed", "Received->Proposed"]);
        replayQueue.Intents.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConflictingDuplicateShouldReturnMetadataOnlyProblemAndSkipAuditAndDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink,
            idempotencyStore: new ConflictingIdempotencyStore());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(409);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Conflict);
        result.Problem.Code.ShouldBe("idempotency_conflict_command_execution");
        result.Problem.Retryable.ShouldBeFalse();
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        replayQueue.Intents.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();
        Serialized(result.Problem).ShouldNotContain(BoundTenant, Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task InvalidLifecycleTransitionShouldBeAuditedAndFailBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink,
            lifecycleTransitionGuard: new FixedLifecycleTransitionGuard(
                LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated"))));

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(409);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Conflict);
        result.Problem.Code.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Decision.ShouldBe("reject");
        auditWriter.Envelopes[0].ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
        auditWriter.Envelopes[0].StateTransition.ShouldBe("Received->Associated");
        replayQueue.Intents.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();
        Serialized(result.Problem).ShouldNotContain(BoundTenant, Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task InvalidLifecycleTransitionAuditUnavailableShouldQueueReplayAlertAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink,
            lifecycleTransitionGuard: new FixedLifecycleTransitionGuard(
                LifecycleTransitionValidation.Invalid(new LifecycleTransitionDefinition("Received", "Associated"))));

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(503);
        result.Problem.Code.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.Count.ShouldBe(1);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task AuditEnvelopeShouldUseGatewayComputedIdempotencyMetadata()
    {
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        ClaimsPrincipal principal = Principal(
            BoundTenant,
            new Claim("idempotency_key", "untrusted-secret-token"));
        CommandGateway gateway = Gateway(
            new RecordingDispatcher(),
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(principal, new TenantScopedCommand(BoundTenant, "allowed-resource")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.IdempotencyKey != null && envelope.IdempotencyKey.Length == 64);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.IdempotencyKey != "untrusted-secret-token");
        JsonSerializer.Serialize(auditWriter.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ShouldNotContain("untrusted-secret-token", Case.Insensitive);
    }

    [Fact]
    public static void CanonicalizationShouldNormalizePropertyOrderWhitespaceAndUnicode()
    {
        using JsonDocument first = JsonDocument.Parse(
            """
            { "name": "caf\u00e9", "items": [1, 2], "nested": { "b": true, "a": "x" } }
            """);
        using JsonDocument second = JsonDocument.Parse(
            """
            {
              "nested": { "a": "x", "b": true },
              "items": [ 1, 2 ],
              "name": "cafe\u0301"
            }
            """);
        using JsonDocument changed = JsonDocument.Parse(
            """
            { "name": "caf\u00e9", "items": [2, 1], "nested": { "b": true, "a": "x" } }
            """);
        using JsonDocument changedValue = JsonDocument.Parse(
            """
            { "name": "caf\u00e9", "items": [1, 2], "nested": { "b": true, "a": "y" } }
            """);

        string firstHash = CoarseIdempotencyCanonicalizer.HashCanonicalJson(first.RootElement);
        string secondHash = CoarseIdempotencyCanonicalizer.HashCanonicalJson(second.RootElement);
        string changedHash = CoarseIdempotencyCanonicalizer.HashCanonicalJson(changed.RootElement);
        string changedValueHash = CoarseIdempotencyCanonicalizer.HashCanonicalJson(changedValue.RootElement);

        secondHash.ShouldBe(firstHash);
        changedHash.ShouldNotBe(firstHash);
        changedValueHash.ShouldNotBe(firstHash);
    }

    [Fact]
    public async Task StateStoreEquivalentRepeatShouldMatchSingleSubmitEndState()
    {
        InMemoryCoarseIdempotencyStore store = new(new FixedClock());
        RecordingDispatcher dispatcher = new();
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: store);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource"));

        ChatBotGatewayResult single = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        CoarseIdempotencyRecord storedAfterSingle = store.Records.Single();
        ChatBotGatewayResult repeat = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        CoarseIdempotencyRecord storedAfterRepeat = store.Records.Single();

        single.IsAccepted.ShouldBeTrue();
        repeat.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        storedAfterRepeat.ShouldBe(storedAfterSingle);
    }

    [Fact]
    public async Task PreCommitAuditUnavailableShouldQueueReplayAlertAndNeverDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new() { PreCommitResult = AuditWriteResult.Unavailable() };
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(503);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Internal_error);
        result.Problem.Code.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        result.Problem.Retryable.ShouldBeTrue();
        result.Problem.ClientAction.ShouldBe(ProblemDetailsClientAction.RetryLater);
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        dispatcher.DispatchCount.ShouldBe(0);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents[0].ReasonCode.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Phase.ShouldBe(AuditCommitPhase.PreCommit);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task DispatchFailureShouldFailClosedAbortAdmissionQueueReplayAndAlert()
    {
        // Regression guard: the real dispatcher throws (EventStore gateway unreachable / non-2xx, or an
        // unreadable payload) AFTER pre-commit audit succeeded but BEFORE any durable state exists. The gateway
        // must fail closed — release the coarse-idempotency admission (so a retry is not poisoned), queue a
        // replay intent, alert the operator, and return a redacted 503 — never an unhandled 500 leaking the
        // exception text and never a dangling admission.
        ThrowingDispatcher dispatcher = new();
        AbortTrackingIdempotencyStore idempotencyStore = new();
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink,
            idempotencyStore: idempotencyStore);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(503);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Internal_error);
        result.Problem.Retryable.ShouldBeTrue();
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        idempotencyStore.AbortCount.ShouldBe(1);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);

        // Pre-commit audit ran (durable intent recorded); post-commit never ran (no durable state was written).
        auditWriter.Envelopes.Count.ShouldBe(1);
        auditWriter.Envelopes[0].Phase.ShouldBe(AuditCommitPhase.PreCommit);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task PostCommitAuditFailureShouldQueueReconciliationAndKeepDispatchAccepted()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new()
        {
            PostCommitResult = AuditWriteResult.Unavailable(AuditFailureReasonCodes.PostCommitAuditFailed),
        };
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alertSink = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alertSink);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        result.AuditReconciliationRequired.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        replayQueue.Intents.Count.ShouldBe(1);
        replayQueue.Intents[0].Kind.ShouldBe(AuditReplayIntentKind.PostCommitAuditReconciliation);
        replayQueue.Intents[0].ReasonCode.ShouldBe(AuditFailureReasonCodes.PostCommitAuditFailed);
        alertSink.Alerts.Count.ShouldBe(1);
        alertSink.Alerts[0].Kind.ShouldBe(OperatorAlertKind.PostCommitAuditReconciliationRequired);
        auditWriter.Envelopes.Select(static envelope => envelope.Phase).ShouldBe(
            [AuditCommitPhase.PreCommit, AuditCommitPhase.PostCommit]);
    }

    [Fact]
    public async Task ReplayShouldPreserveReconcilingAuditStatusAndNeverDowngradeToCommitted()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new()
        {
            PostCommitResult = AuditWriteResult.Unavailable(AuditFailureReasonCodes.PostCommitAuditFailed),
        };
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            operationStatusStore: statusStore);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource"));

        ChatBotGatewayResult first = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        OperationStatusRecord? afterFirst = await statusStore.TryGetAsync(BoundTenant, TaskId, TestContext.Current.CancellationToken);
        ChatBotGatewayResult replay = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        OperationStatusRecord? afterReplay = await statusStore.TryGetAsync(BoundTenant, TaskId, TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        first.AuditReconciliationRequired.ShouldBeTrue();
        replay.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);

        afterFirst.ShouldNotBeNull();
        afterFirst.AuditStatus.ShouldBe(OperationStatusRecord.AuditReconciling);

        // The idempotent replay must resolve to the SAME operation-status record and must never report audit as
        // 'committed' while the post-commit reconciliation is still pending (never a false Done).
        afterReplay.ShouldNotBeNull();
        afterReplay.OperationId.ShouldBe(afterFirst.OperationId);
        afterReplay.AuditStatus.ShouldBe(OperationStatusRecord.AuditReconciling);
        afterReplay.AuditStatus.ShouldNotBe(OperationStatusRecord.AuditCommitted);
        afterReplay.CompletionStatus.ShouldBe(OperationStatusRecord.AcceptedProjectionPending);
    }

    [Fact]
    public async Task AuditEnvelopesShouldContainRequiredFieldsAndRemainMetadataOnly()
    {
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(new RecordingDispatcher(), auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant),
                new TenantScopedCommand(
                    BoundTenant,
                    "payload-sentinel wrong-tenant project-alpha file-secret.txt raw exception /home/administrator/local-path")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        auditWriter.Envelopes.Count.ShouldBe(2);
        foreach (AuditEnvelope envelope in auditWriter.Envelopes)
        {
            envelope.TenantId.ShouldBe(BoundTenant);
            envelope.ActorId.ShouldBe(ActorId);
            envelope.ActorType.ShouldBe("user");
            envelope.CommandName.ShouldBe(nameof(TenantScopedCommand));
            envelope.ResourceId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
            envelope.Decision.ShouldNotBeNullOrWhiteSpace();
            envelope.ReasonCode.ShouldNotBeNullOrWhiteSpace();
            envelope.CorrelationId.ShouldBe(CorrelationId);
            envelope.Timestamp.ShouldBe(FixedClock.FixedUtcNow);
            envelope.PolicySnapshotId.ShouldNotBeNullOrWhiteSpace();
            envelope.SourceEvidenceRefs.ShouldNotBeEmpty();
            envelope.StateTransition.ShouldNotBeNullOrWhiteSpace();
            envelope.RedactionDecision.ShouldBe("metadata_only");
            envelope.Outcome.ShouldNotBeNullOrWhiteSpace();
            envelope.EnvelopeSchemaVersion.ShouldNotBeNullOrWhiteSpace();
            envelope.SurfaceOrigin.ShouldBe("api");
        }

        string serialized = JsonSerializer.Serialize(auditWriter.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        serialized.ShouldNotContain("payload-sentinel", Case.Insensitive);
        serialized.ShouldNotContain("wrong-tenant", Case.Insensitive);
        serialized.ShouldNotContain("project-alpha", Case.Insensitive);
        serialized.ShouldNotContain("file-secret.txt", Case.Insensitive);
        serialized.ShouldNotContain("raw exception", Case.Insensitive);
        serialized.ShouldNotContain("/home/administrator/local-path", Case.Insensitive);
    }

    [Fact]
    public async Task SurfaceOriginDeclaredAtBoundaryShouldAppearImmutablyInEveryAuditEnvelope()
    {
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(new RecordingDispatcher(), auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "ui");

        // The pre-commit and post-commit envelopes carry the identical origin: it is captured once at
        // the boundary on the immutable submission and no downstream stage can rewrite it.
        auditWriter.Envelopes.Select(static envelope => envelope.SurfaceOrigin).Distinct(StringComparer.Ordinal).Count().ShouldBe(1);
    }

    [Fact]
    public async Task UnknownSurfaceOriginShouldCollapseToSafeDefaultAndStillBeAudited()
    {
        ChatBotSurfaceOrigin resolved = ChatBotSurfaceOrigins.FromWireValueOrDefault("totally-unknown-surface");
        resolved.ShouldBe(ChatBotSurfaceOrigin.Api);

        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(new RecordingDispatcher(), auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource"), origin: resolved),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        auditWriter.Envelopes.ShouldNotBeEmpty();
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "api");
    }

    [Fact]
    public async Task NonAllowlistedCommandShouldBeRejectedFailClosedWithNoDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel project-alpha")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Authorization_denied);
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.RefusalBlockedAction);
        result.Problem.Retryable.ShouldBeFalse();
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);

        // Fail-closed BEFORE any durable-state work: no dispatch, no pre/post-commit audit envelope, and —
        // critically — no coarse-idempotency admission was ever recorded (the allowlist gate runs before
        // RecordAdmission), so a rejected non-allowlisted submission leaves no admission to leak or replay.
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        idempotencyStore.Records.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("project-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task DownstreamStageAttemptingToRewriteSurfaceOriginShouldNotChangeTheAuditedOrigin()
    {
        // A buggy/malicious downstream stage tries to overwrite the boundary-declared origin. Because
        // ChatBotCommandSubmission is an immutable record, the attempt can only produce a separate discarded
        // copy — it cannot replace the submission the gateway holds — so every audit envelope still carries the
        // origin captured at the boundary (FR85 / S7).
        OriginTamperingRiskClassifier tamperer = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            tamperer,
            new PassThroughApprovalGate(),
            new InMemoryCoarseIdempotencyStore(new FixedClock()),
            auditWriter,
            new RecordingReplayIntentQueue(),
            new RecordingOperatorAlertSink(),
            new InMemoryOperationStatusStore(),
            new FixedClock(),
            new CommandSubmissionLifecycleTransitionGuard(),
            new RecordingDispatcher(),
            DefaultProblemDetailsFactory(),
            new PermissiveSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "allowed-resource"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        tamperer.OriginObservedAfterTamperAttempt.ShouldBe(ChatBotSurfaceOrigin.Ui);
        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.SurfaceOrigin == "ui");
    }

    [Fact]
    public async Task FailClosedDenialAuditFactShouldRecordTheDeclaredSurfaceOrigin()
    {
        // The fail-closed denial fact (here: command-not-allowlisted) must carry the boundary surface origin so
        // a denied attempt is attributable to its surface, not just the admitted ones.
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(
            new RecordingDispatcher(),
            auditWriter: auditWriter,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
        auditWriter.AuthorizationFailures[0].SurfaceOrigin.ShouldBe("ui");
    }

    [Fact]
    public async Task AllowlistedTrivialCommandShouldBeAdmittedAndDispatched()
    {
        RecordingDispatcher dispatcher = new();
        CommandGateway gateway = Gateway(dispatcher, commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task AiActionProposalRiskStageShouldAttachMetadataOnlyAuditRefsBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                AiProposalCommand()),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.Envelopes.Count.ShouldBe(2);
        foreach (AuditEnvelope envelope in auditWriter.Envelopes)
        {
            envelope.SourceEvidenceRefs.ShouldContain("classifier:chatbot.ai-action-risk-classifier.m0.v1");
            envelope.SourceEvidenceRefs.ShouldContain("risk-class:approval-required");
            envelope.SourceEvidenceRefs.ShouldContain("risk-action:modifies-state");
            envelope.SourceEvidenceRefs.ShouldContain("reason:risky_action_class");
        }
    }

    [Fact]
    public async Task AiActionProposalUnsupportedMetadataShouldFailClosedBeforeDurableWork()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                AiProposalCommand("Project.UnknownCommand") with { CommandMetadataSupported = false }),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.RefusalBlockedAction);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
        idempotencyStore.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task AiActionProposalShouldNotTrustCallerSuppliedLowRiskMetadataForUnknownCommand()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        var spoofedLowRisk = AiProposalCommand("Project.UnknownReadCommand") with
        {
            ProposedActionClasses = [],
            EffectSurface = "read-only",
            TenantPolicyClassification = "low-risk",
            CommandAllowlistVersion = "ai-action-command-allowlist.spoofed",
            CommandDefaultRisk = Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass.LowRisk,
            CommandMetadataSupported = true,
        };

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                spoofedLowRisk),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.RefusalBlockedAction);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        idempotencyStore.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task LowRiskAiExecutionPolicyFalseShouldPersistApprovalRouteWithoutProviderExecution()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            approvalGate: new AiActionApprovalGate(
                new DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(false))),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                LowRiskExecutionCommand()),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);
        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("low-risk-policy-reason:low_risk_policy_false") &&
            envelope.SourceEvidenceRefs.Contains("context-package:context-package-001") &&
            envelope.SourceEvidenceRefs.Contains("execution:ai-execution-001"));
    }

    [Fact]
    public async Task LowRiskAiExecutionPolicyAllowedShouldProceedThroughAuditAndDispatchWithPolicyRefs()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            approvalGate: new AiActionApprovalGate(
                new DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(true))),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                LowRiskExecutionCommand()),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("low-risk-policy-reason:low-risk-execute-allowed") &&
            envelope.SourceEvidenceRefs.Contains("context-package:context-package-001") &&
            envelope.SourceEvidenceRefs.Contains("execution:ai-execution-001"));
    }

    [Fact]
    public async Task LowRiskAiExecutionShouldRequireExplicitProjectAuthorization()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            authorizationStage: new ParticipantAuthorizationStage(),
            idempotencyStore: idempotencyStore,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            approvalGate: new AiActionApprovalGate(
                new DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(true))),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                LowRiskExecutionCommand()),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotencyStore.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task LowRiskAiExecutionReplayShouldIgnoreClientChangedExecutionIdForSameLogicalRequest()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            riskClassifier: new DeterministicAiActionRiskClassifier(),
            approvalGate: new AiActionApprovalGate(
                new DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(true))),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                LowRiskExecutionCommand("ai-execution-001")),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult replay = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                LowRiskExecutionCommand("ai-execution-retry-002")),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        replay.IsAccepted.ShouldBeTrue();
        replay.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);
        replay.Accepted.CorrelationId.ShouldBe(first.Accepted.CorrelationId);
        replay.Accepted.TaskId.ShouldBe(first.Accepted.TaskId);
        replay.Accepted.AcceptedAt.ShouldBe(first.Accepted.AcceptedAt);
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);
    }

    [Fact]
    public async Task ApprovalDecisionShouldUseSharedSpineAndApprovalDecisionIdempotency()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            approvalGate: new AiActionApprovalGate(new DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(true))),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-approver")),
                ApprovalDecisionCommand()),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult replay = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-approver")),
                ApprovalDecisionCommand(decisionId: "approval-decision-002")),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        replay.IsAccepted.ShouldBeTrue();
        replay.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.Records.ShouldHaveSingleItem().OperationClass.ShouldBe(CoarseIdempotencyOperationClass.ApprovalDecision.Code);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Contains("approval:approval:ai-proposal-001") &&
            envelope.SourceEvidenceRefs.Contains("approval-decision:approve"));
    }

    [Fact]
    public async Task ApprovalApproveShouldRequireApprovalAuthorityBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            approvalGate: new AiActionApprovalGate(new DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(true))),
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant, new Claim("requester_authority_class", "project-contributor")),
                ApprovalDecisionCommand()),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        dispatcher.DispatchCount.ShouldBe(0);
        idempotencyStore.RecordCount.ShouldBe(0);
    }

    [Fact]
    public async Task AllowlistedMailboxIntakeShouldUseMessageIntakeIdempotencyAndDispatch()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), MailboxCommand(), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        CoarseIdempotencyRecord record = idempotencyStore.Records.Single();
        record.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.MessageIntake.Code);
        record.CommandType.ShouldBe(typeof(Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake).Name);
        record.CoarseKeyHash.Length.ShouldBe(64);
        record.CanonicalEquivalenceHash.ShouldBe(record.CoarseKeyHash);
    }

    [Fact]
    public async Task DuplicateMailboxProviderDeliveryShouldReplayPriorOutcomeAuditSuppressionAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), MailboxCommand("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult second = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), MailboxCommand("01ARZ3NDEKTSV4RRFFQ69G5FBA"), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        second.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);
        second.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);
        auditWriter.Envelopes.ShouldContain(static envelope =>
            envelope.ReasonCode == "duplicate_provider_message" &&
            envelope.Outcome == "duplicate_suppressed" &&
            envelope.SurfaceOrigin == "mailbox");
    }

    [Fact]
    public async Task DuplicateMailboxProviderDeliveryShouldRefreshOnlySafeDuplicateStatusMetadata()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            operationStatusStore: statusStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), MailboxCommand("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);
        _ = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), MailboxCommand("01ARZ3NDEKTSV4RRFFQ69G5FBA"), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);

        OperationStatusRecord status = (await statusStore
            .TryGetAsync(BoundTenant, OperationStatusRecord.OperationIdFor(first.Accepted!), TestContext.Current.CancellationToken))
            .ShouldNotBeNull();

        status.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.MessageIntake.Code);
        status.OriginalOperationId.ShouldBe(OperationStatusRecord.OperationIdFor(first.Accepted!));
        status.DuplicateAttemptCount.ShouldBe(1);
        status.DuplicateSafetyNote.ShouldBe("duplicate-provider-message-suppressed");
        status.SafeNextActions.ShouldContain(ChatBotMessageNextActions.None);
        dispatcher.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task RetryCommandShouldUseFailedEventAndActorScopedIdempotency()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), RetryCommand(note: "first safe note"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult replay = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), RetryCommand(note: "second safe note"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        replay.IsAccepted.ShouldBeTrue();
        replay.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);
        dispatcher.DispatchCount.ShouldBe(1);

        CoarseIdempotencyRecord record = idempotencyStore.Records.Single();
        record.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.Retry.Code);
        record.ExpiresAt.ShouldBe(DateTimeOffset.MaxValue);
    }

    [Fact]
    public async Task ConflictingRetryAttemptShouldReturnRetryConflictAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), RetryCommand(reasonCode: "graph_throttled"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult conflict = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), RetryCommand(reasonCode: "graph_token_expired"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        conflict.IsAccepted.ShouldBeFalse();
        conflict.Problem.ShouldNotBeNull();
        conflict.Problem.Code.ShouldBe("idempotency_conflict_retry");
        dispatcher.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task MailboxIntakePreCommitAuditUnavailableShouldAbortMessageIntakeAdmissionQueueReplayAndAlert()
    {
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            new RecordingDispatcher(),
            auditWriter: new RecordingAuditWriter { PreCommitResult = AuditWriteResult.Unavailable() },
            replayQueue: replayQueue,
            alertSink: alerts,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), MailboxCommand(), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem!.Status.ShouldBe(503);
        idempotencyStore.RecordCount.ShouldBe(0);
        replayQueue.Intents.Single().Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        alerts.Alerts.Single().Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);
    }

    [Fact]
    public async Task MailboxIntakeIdempotencyShouldNormalizeMailboxAndProviderIdsBeforeHashing()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant),
                MailboxCommand(
                    intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    mailboxId: "controlled-mailbox-caf\u00e9",
                    providerMessageId: "graph-message-cafe\u0301"),
                origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult second = await gateway.SubmitAsync(
            Submission(
                Principal(BoundTenant),
                MailboxCommand(
                    intakeId: "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                    mailboxId: "controlled-mailbox-cafe\u0301",
                    providerMessageId: "graph-message-caf\u00e9"),
                origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        second.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);
        second.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);
        auditWriter.Envelopes.ShouldContain(static envelope =>
            envelope.ReasonCode == "duplicate_provider_message" &&
            envelope.Outcome == "duplicate_suppressed");
    }

    [Fact]
    public void AssociationScoringIdempotencyShouldUseDefaultKernelForEmptySubmissionKernel()
    {
        ChatBotAuthenticatedActor actor = new(ActorId, Principal(BoundTenant));
        ChatBotTenantBinding binding = new(BoundTenant);
        CoarseIdempotencyRecord implicitKernel = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(
            new ChatBotGatewayContext(
                Submission(Principal(BoundTenant), AssociationScoringCommand(string.Empty), origin: ChatBotSurfaceOrigin.Mailbox),
                actor,
                binding),
            new FixedClock().UtcNow);
        CoarseIdempotencyRecord explicitKernel = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(
            new ChatBotGatewayContext(
                Submission(Principal(BoundTenant), AssociationScoringCommand(DeterministicAssociationScorer.CurrentKernelVersion), origin: ChatBotSurfaceOrigin.Mailbox),
                actor,
                binding),
            new FixedClock().UtcNow);

        implicitKernel.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.AssociationScoring.Code);
        explicitKernel.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.AssociationScoring.Code);
        implicitKernel.CoarseKeyHash.ShouldBe(explicitKernel.CoarseKeyHash);
        implicitKernel.CanonicalEquivalenceHash.ShouldBe(explicitKernel.CanonicalEquivalenceHash);
    }

    [Fact]
    public async Task AssociationDecisionShouldUseTwentyFourHourActorScopedIdempotencyAndUiAuditOrigin()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationDecisionCommand(), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult duplicate = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationDecisionCommand(commandNote: "Reviewed same safe metadata."), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        duplicate.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        duplicate.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);

        CoarseIdempotencyRecord record = idempotencyStore.Records.Single();
        record.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.AssociationDecision.Code);
        record.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject));
        record.CoarseKeyHash.Length.ShouldBe(64);
        record.CanonicalEquivalenceHash.Length.ShouldBe(64);
        record.CanonicalEquivalenceHash.ShouldNotBe(record.CoarseKeyHash);
        record.ExpiresAt.ShouldBe(FixedClock.FixedUtcNow.AddHours(24));

        auditWriter.Envelopes.Select(static envelope => envelope.SurfaceOrigin).ShouldBe(["ui", "ui"]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Any(static reference => reference.Contains("hash-project", StringComparison.Ordinal)));

        string serializedAudit = JsonSerializer.Serialize(auditWriter.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        serializedAudit.ShouldNotContain("Reviewed same safe metadata.", Case.Insensitive);
        serializedAudit.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task ConflictingAssociationDecisionShouldReturnMetadataOnlyConflictAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationDecisionCommand(), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult conflict = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationDecisionCommand(projectId: "project-002"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        conflict.IsAccepted.ShouldBeFalse();
        conflict.Problem.ShouldNotBeNull();
        conflict.Problem.Status.ShouldBe(409);
        conflict.Problem.Code.ShouldBe("idempotency_conflict_association_decision");
        dispatcher.DispatchCount.ShouldBe(1);
        idempotencyStore.RecordCount.ShouldBe(1);

        string serialized = Serialized(conflict.Problem);
        serialized.ShouldNotContain("project-001", Case.Insensitive);
        serialized.ShouldNotContain("project-002", Case.Insensitive);
        serialized.ShouldNotContain("Reviewed safe metadata.", Case.Insensitive);
    }

    [Fact]
    public async Task AssociationDecisionPreCommitAuditUnavailableShouldAbortAdmissionQueueReplayAndSkipDispatch()
    {
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            new RecordingDispatcher(),
            auditWriter: new RecordingAuditWriter { PreCommitResult = AuditWriteResult.Unavailable() },
            replayQueue: replayQueue,
            alertSink: alerts,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationDecisionCommand(commandNote: "metadata-only rationale"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(503);
        result.Problem.Code.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        idempotencyStore.RecordCount.ShouldBe(0);
        replayQueue.Intents.Single().Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents.Single().CommandName.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject));
        alerts.Alerts.Single().Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);

        Serialized(result.Problem).ShouldNotContain("metadata-only rationale", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("project-001", Case.Insensitive);
    }

    [Fact]
    public async Task AssociationCorrectionShouldUseIndefiniteActorScopedIdempotencyAndUiAuditOrigin()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationCorrectionCommand(), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult duplicate = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationCorrectionCommand(rationale: "Same safe metadata."), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        duplicate.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        duplicate.Accepted!.CommandId.ShouldBe(first.Accepted!.CommandId);

        CoarseIdempotencyRecord record = idempotencyStore.Records.Single();
        record.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.Correction.Code);
        record.CommandType.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation));
        record.ExpiresAt.ShouldBe(DateTimeOffset.MaxValue);
        auditWriter.Envelopes.Select(static envelope => envelope.SurfaceOrigin).ShouldBe(["ui", "ui"]);
        auditWriter.Envelopes.ShouldAllBe(static envelope =>
            envelope.SourceEvidenceRefs.Any(static reference => reference.Contains("hash-project-002", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ConflictingAssociationCorrectionShouldReturnMetadataOnlyConflictAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult first = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationCorrectionCommand(), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult conflict = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationCorrectionCommand(targetProjectId: "project-003"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        first.IsAccepted.ShouldBeTrue();
        conflict.IsAccepted.ShouldBeFalse();
        conflict.Problem.ShouldNotBeNull();
        conflict.Problem.Status.ShouldBe(409);
        conflict.Problem.Code.ShouldBe("idempotency_conflict_correction");
        dispatcher.DispatchCount.ShouldBe(1);

        string serialized = Serialized(conflict.Problem);
        serialized.ShouldNotContain("project-002", Case.Insensitive);
        serialized.ShouldNotContain("project-003", Case.Insensitive);
        serialized.ShouldNotContain("Wrong project", Case.Insensitive);
    }

    [Fact]
    public async Task AssociationCorrectionPreCommitAuditUnavailableShouldAbortAdmissionQueueReplayAndSkipDispatch()
    {
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            new RecordingDispatcher(),
            auditWriter: new RecordingAuditWriter { PreCommitResult = AuditWriteResult.Unavailable() },
            replayQueue: replayQueue,
            alertSink: alerts,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), AssociationCorrectionCommand(rationale: "metadata-only correction rationale"), origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(503);
        result.Problem.Code.ShouldBe(AuditFailureReasonCodes.AuditUnavailable);
        idempotencyStore.RecordCount.ShouldBe(0);
        replayQueue.Intents.Single().Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents.Single().CommandName.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation));
        alerts.Alerts.Single().Kind.ShouldBe(OperatorAlertKind.AuditUnavailable);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain("metadata-only correction rationale", Case.Insensitive);
        serialized.ShouldNotContain("project-002", Case.Insensitive);
        serialized.ShouldNotContain("hash-project-002", Case.Insensitive);
    }

    [Fact]
    public async Task MailboxIntakeMissingTenantContextShouldFailClosedBeforeDurableStateWork()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        RecordingReplayIntentQueue replayQueue = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            auditWriter: auditWriter,
            replayQueue: replayQueue,
            alertSink: alerts,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(null), MailboxCommand(), origin: ChatBotSurfaceOrigin.Mailbox),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Single().SurfaceOrigin.ShouldBe("mailbox");
        replayQueue.Intents.Single().Kind.ShouldBe(AuditReplayIntentKind.PreCommitOperationReplay);
        replayQueue.Intents.Single().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);
        replayQueue.Intents.Single().TenantId.ShouldBe("unresolved");
        replayQueue.Intents.Single().CommandName.ShouldBe(nameof(Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake));
        alerts.Alerts.Single().Kind.ShouldBe(OperatorAlertKind.TenantScopeUnresolved);
        alerts.Alerts.Single().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain("graph-message-001", Case.Insensitive);
        serialized.ShouldNotContain("controlled-mailbox-001", Case.Insensitive);
    }

    [Fact]
    public async Task AuditEnvelopeShouldNormalizeUntrustedAuditMetadata()
    {
        RecordingAuditWriter auditWriter = new();
        using JsonDocument command = JsonDocument.Parse(
            """
            {
              "tenantId": "tenant-alpha",
              "resourceName": "payload-sentinel raw exception /home/administrator/project-secret.txt"
            }
            """);
        ClaimsPrincipal principal = Principal(
            BoundTenant,
            new Claim("actor_type", "raw exception /home/administrator/project-secret.txt"),
            new Claim("idempotency_key", "secret-token-/home/administrator/project-secret.txt"));
        CommandGateway gateway = Gateway(new RecordingDispatcher(), auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                principal,
                command.RootElement.Clone(),
                "raw exception /home/administrator/project-secret.txt"),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeTrue();
        auditWriter.Envelopes.Count.ShouldBe(2);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.ActorType == "user");
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.IdempotencyKey != null && envelope.IdempotencyKey.Length == 64);
        auditWriter.Envelopes.ShouldAllBe(static envelope => envelope.CommandName == AuditMetadata.UnknownCommandName);

        string serialized = JsonSerializer.Serialize(auditWriter.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        serialized.ShouldNotContain("payload-sentinel", Case.Insensitive);
        serialized.ShouldNotContain("raw exception", Case.Insensitive);
        serialized.ShouldNotContain("/home/administrator", Case.Insensitive);
        serialized.ShouldNotContain("project-secret.txt", Case.Insensitive);
        serialized.ShouldNotContain("secret-token", Case.Insensitive);
    }

    [Fact]
    public static void StateWritingPathInventoryShouldDriveFailClosedAuditCoverage()
    {
        string[] expectedCodes =
        [
            "m365-mailbox-intake",
            "deterministic-association",
            "ambiguous-user-association",
            "correction",
            "ai-action-proposal",
            "approval-decision",
            "command-execution",
            "outbound-send",
            "tenant-policy-mutation",
            "allowlist-mutation",
        ];

        ChatBotStateWritingPathInventory.Paths
            .Select(static path => path.Code)
            .ShouldBe(expectedCodes, ignoreOrder: false);
        ChatBotStateWritingPathInventory.Paths
            .Select(static path => path.Code)
            .Distinct(StringComparer.Ordinal)
            .Count()
            .ShouldBe(ChatBotStateWritingPathInventory.Paths.Count);
        ChatBotStateWritingPathInventory.Paths
            .ShouldAllBe(static path => string.Equals(
                path.AuditCommitSeam,
                ChatBotStateWritingPathInventory.RequiredAuditCommitSeam,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingAuthenticationShouldReturnSafeProblemDetailsAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(new ClaimsPrincipal(new ClaimsIdentity()), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(401);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Authentication_failure);
        result.Problem.Code.ShouldBe("authentication_denied");
        result.Problem.CorrelationId.ShouldBe(CorrelationId);
        result.Problem.TaskId.ShouldBe(TaskId);
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task MissingTenantContextShouldReturnSafeProblemDetailsAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(null), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Category.ShouldBe(ProblemDetailsCategory.Authorization_denied);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);
        Serialized(result.Problem).ShouldNotContain(BoundTenant, Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task AmbiguousTenantContextShouldFailClosedAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(PrincipalWithTenantClaims(BoundTenant, OtherTenant), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain(OtherTenant, Case.Insensitive);
        serialized.ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task MalformedTenantContextShouldFailClosedAndSkipDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(PrincipalWithTenantClaims("tenant alpha"), new TenantScopedCommand(BoundTenant, "payload-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        auditWriter.AuthorizationFailures[0].ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMissing);
        Serialized(result.Problem).ShouldNotContain("tenant alpha", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("payload-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CrossTenantTargetMismatchShouldAuditOnceAndNeverDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(OtherTenant, "restricted-project-sentinel")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures[0];
        fact.TenantId.ShouldBe(BoundTenant);
        fact.ActorId.ShouldBe(ActorId);
        fact.CommandType.ShouldBe(nameof(TenantScopedCommand));
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        fact.CorrelationId.ShouldBe(CorrelationId);
        fact.TaskId.ShouldBe(TaskId);
        fact.SurfaceOrigin.ShouldBe("api");

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain(OtherTenant, Case.Insensitive);
        serialized.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CrossTenantScopedIdentifierMismatchShouldAuditOnceAndNeverDispatch()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: auditWriter);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedIdentifierCommand($"{OtherTenant}:projects:project-1")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Status.ShouldBe(403);
        result.Problem.Code.ShouldBe("authorization_denied");
        dispatcher.DispatchCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Count.ShouldBe(1);
        ChatBotAuthorizationFailureAuditFact fact = auditWriter.AuthorizationFailures[0];
        fact.TenantId.ShouldBe(BoundTenant);
        fact.ActorId.ShouldBe(ActorId);
        fact.CommandType.ShouldBe(nameof(TenantScopedIdentifierCommand));
        fact.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.TenantMismatch);
        fact.CorrelationId.ShouldBe(CorrelationId);
        fact.TaskId.ShouldBe(TaskId);

        string serialized = Serialized(result.Problem);
        serialized.ShouldNotContain(BoundTenant, Case.Insensitive);
        serialized.ShouldNotContain(OtherTenant, Case.Insensitive);
        serialized.ShouldNotContain("project-1", Case.Insensitive);
    }

    [Fact]
    public async Task AuthorizationDeniedAndSafeNotFoundShouldBeIndistinguishableToCaller()
    {
        RecordingDispatcher deniedDispatcher = new();
        RecordingDispatcher hiddenDispatcher = new();
        CommandGateway deniedGateway = Gateway(
            deniedDispatcher,
            authorizationStage: new DenyingAuthorizationStage(ChatBotAuthorizationReasonCodes.AuthorizationDenied));
        CommandGateway hiddenGateway = Gateway(
            hiddenDispatcher,
            authorizationStage: new DenyingAuthorizationStage(ChatBotAuthorizationReasonCodes.SafeNotFound));

        ChatBotGatewayResult denied = await deniedGateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "denied-resource")),
            TestContext.Current.CancellationToken);
        ChatBotGatewayResult hidden = await hiddenGateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "missing-resource")),
            TestContext.Current.CancellationToken);

        deniedDispatcher.DispatchCount.ShouldBe(0);
        hiddenDispatcher.DispatchCount.ShouldBe(0);
        denied.Problem.ShouldNotBeNull();
        hidden.Problem.ShouldNotBeNull();
        denied.Problem.Status.ShouldBe(hidden.Problem.Status);
        denied.Problem.Category.ShouldBe(hidden.Problem.Category);
        denied.Problem.Code.ShouldBe(hidden.Problem.Code);
        denied.Problem.Message.ShouldBe(hidden.Problem.Message);
        denied.Problem.ClientAction.ShouldBe(hidden.Problem.ClientAction);
        denied.Problem.Retryable.ShouldBe(hidden.Problem.Retryable);
        denied.Problem.Details.Visibility.ShouldBe(hidden.Problem.Details.Visibility);
    }

    [Fact]
    public static void ProblemDetailsFactoryShouldReturnCatalogBackedCurrentGatewayProblems()
    {
        IChatBotProblemDetailsFactory factory = DefaultProblemDetailsFactory();

        ProblemDetails[] problems =
        [
            factory.CreateAuthorizationProblem(ChatBotAuthorizationReasonCodes.AuthenticationDenied, CorrelationId, TaskId),
            factory.CreateAuthorizationProblem(ChatBotAuthorizationReasonCodes.AuthorizationDenied, CorrelationId, TaskId),
            factory.CreateAuditUnavailable(CorrelationId, TaskId),
            factory.CreateIdempotencyConflict(CorrelationId, TaskId),
            factory.CreateInvalidLifecycleTransition(CorrelationId, TaskId),
        ];

        foreach (ProblemDetails problem in problems)
        {
            ChatBotMessageCatalogEntry entry = ChatBotMessageCatalog.Resolve(problem.Code);
            problem.Title.ShouldBe(entry.Headline);
            problem.Message.ShouldBe(entry.Reason);
            CatalogClientAction(problem.ClientAction).ShouldBe(entry.NextAction);
            problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
            Serialized(problem).ShouldNotContain("contact_support", Case.Insensitive);
        }
    }

    [Fact]
    public static void ProblemDetailsFactoryShouldRecordUncategorizedAuthorizationReasonWithoutRawInput()
    {
        InMemoryUserFacingMessageTelemetry telemetry = new();
        ChatBotProblemDetailsFactory factory = new(new CoarseUserFacingRedactionStage(), telemetry);
        string rawReason = "tenant-alpha project-alpha file-secret.txt party-alpha audit detail payload secret C:\\temp\\raw.txt /home/raw InvalidOperationException";

        ProblemDetails problem = factory.CreateAuthorizationProblem(rawReason, CorrelationId, TaskId);

        problem.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        problem.Message.ShouldBe(ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AuthorizationDenied).Reason);
        telemetry.Counts.ShouldContainKey((ChatBotMessageCatalogVersion.Current, ChatBotMessageCodes.AuthorizationDenied));
        telemetry.Counts[(ChatBotMessageCatalogVersion.Current, ChatBotMessageCodes.AuthorizationDenied)].ShouldBe(1);

        string serializedTelemetry = string.Join(
            "|",
            telemetry.Counts.Keys.Select(static key => $"{key.CatalogVersion}:{key.FallbackCode}"));
        serializedTelemetry.ShouldNotContain(rawReason, Case.Insensitive);
        Serialized(problem).ShouldNotContain("project-alpha", Case.Insensitive);
        Serialized(problem).ShouldNotContain("InvalidOperationException", Case.Insensitive);
    }

    [Fact]
    public static void RedactionStageShouldStripNonCatalogDetailAndInstance()
    {
        CoarseUserFacingRedactionStage stage = new();
        ProblemDetails problem = new()
        {
            Type = "https://hexalith.dev/errors/chatbot/audit-unavailable",
            Title = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AuditUnavailable).Headline,
            Status = 503,
            Detail = "raw exception tenant-alpha project-alpha file-secret.txt secret /home/raw C:\\temp\\raw.txt",
            Instance = "/home/administrator/projects/hexalith/chatbot/local-instance",
            Category = ProblemDetailsCategory.Internal_error,
            Code = ChatBotMessageCodes.AuditUnavailable,
            Message = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AuditUnavailable).Reason,
            CorrelationId = CorrelationId,
            TaskId = TaskId,
            Retryable = true,
            ClientAction = ProblemDetailsClientAction.RetryLater,
            Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
        };

        ProblemDetails redacted = stage.Apply(problem);

        redacted.ShouldNotBeSameAs(problem);
        problem.Detail.ShouldNotBeNull();
        problem.Instance.ShouldNotBeNull();
        redacted.Detail.ShouldBeNull();
        redacted.Instance.ShouldBeNull();
        redacted.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        Serialized(redacted).ShouldNotContain("raw exception", Case.Insensitive);
        Serialized(redacted).ShouldNotContain("/home/administrator", Case.Insensitive);
        Serialized(redacted).ShouldNotContain("C:\\temp", Case.Insensitive);
    }

    [Theory]
    [InlineData(
        ParticipantAuthorizationStage.UnresolvedValue,
        ChatBotAuthorizationReasonCodes.UnresolvedParticipant,
        ChatBotMessageCodes.UnresolvedParticipant,
        ProblemDetailsClientAction.RequestAccess)]
    [InlineData(
        ParticipantAuthorizationStage.EmailOnlyValue,
        ChatBotAuthorizationReasonCodes.UnauthorizedParticipant,
        ChatBotMessageCodes.UnauthorizedParticipant,
        ProblemDetailsClientAction.RequestAccess)]
    [InlineData(
        ParticipantAuthorizationStage.UnauthorizedValue,
        ChatBotAuthorizationReasonCodes.UnauthorizedParticipant,
        ChatBotMessageCodes.UnauthorizedParticipant,
        ProblemDetailsClientAction.RequestAccess)]
    [InlineData(
        ParticipantAuthorizationStage.DirectoryDegradedValue,
        ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded,
        ChatBotMessageCodes.ParticipantDirectoryDegraded,
        ProblemDetailsClientAction.RetryLater)]
    public async Task ParticipantAuthorizationShouldBlockBeforeDurableMutationAndReturnCatalogBackedProblem(
        string authority,
        string expectedReasonCode,
        string expectedMessageCode,
        ProblemDetailsClientAction expectedClientAction)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            authorizationStage: new ParticipantAuthorizationStage(),
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore);

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(
                    BoundTenant,
                    new Claim(ParticipantAuthorizationStage.ParticipantAuthorityClaim, authority),
                    new Claim("party", "party-alpha"),
                    new Claim("email", "sender@example.test")),
                new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ")),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Code.ShouldBe(expectedMessageCode);
        result.Problem.ClientAction.ShouldBe(expectedClientAction);
        result.Problem.Details.Visibility.ShouldBe(ProblemDetailsDetailsVisibility.Metadata_only);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.Envelopes.ShouldBeEmpty();
        auditWriter.AuthorizationFailures.Single().ReasonCode.ShouldBe(expectedReasonCode);
        Serialized(result.Problem).ShouldNotContain(BoundTenant, Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("party-alpha", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Theory]
    [InlineData("project-002")]
    [InlineData("project-001")]
    public async Task AssociationCorrectionAuthorizationShouldRequireSourceAndTargetProjectOwnership(string ownedProjectId)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            authorizationStage: new ParticipantAuthorizationStage(),
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(
                    BoundTenant,
                    new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                    new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, ownedProjectId)),
                AssociationCorrectionCommand(),
                origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.AssociationCorrectionTargetUnauthorizedSuppressed);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Single().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AssociationCorrectionTargetUnauthorized);
        Serialized(result.Problem).ShouldNotContain("project-001", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("project-002", Case.Insensitive);
    }

    [Fact]
    public async Task AssociationCorrectionProjectionDependencyUnavailableShouldFailClosedBeforeDurableMutation()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(new FixedClock());
        CommandGateway gateway = Gateway(
            dispatcher,
            authorizationStage: new ParticipantAuthorizationStage(new FixedCorrectionDependencyReadiness(false)),
            auditWriter: auditWriter,
            idempotencyStore: idempotencyStore,
            commandAllowlist: new ChatBotSpineCommandAllowlist());

        ChatBotGatewayResult result = await gateway.SubmitAsync(
            Submission(
                Principal(
                    BoundTenant,
                    new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                    new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
                    new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-002")),
                AssociationCorrectionCommand(),
                origin: ChatBotSurfaceOrigin.Ui),
            TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem.Code.ShouldBe(ChatBotMessageCodes.AssociationCorrectionProjectionUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotencyStore.RecordCount.ShouldBe(0);
        auditWriter.AuthorizationFailures.Single().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AssociationCorrectionProjectionUnavailable);
        Serialized(result.Problem).ShouldNotContain("project-001", Case.Insensitive);
        Serialized(result.Problem).ShouldNotContain("project-002", Case.Insensitive);
    }

    private static string CatalogClientAction(ProblemDetailsClientAction action)
        => action switch
        {
            ProblemDetailsClientAction.Authenticate => ChatBotMessageNextActions.Authenticate,
            ProblemDetailsClientAction.RetryLater => ChatBotMessageNextActions.RetryLater,
            ProblemDetailsClientAction.RequestAccess => ChatBotMessageNextActions.RequestAccess,
            ProblemDetailsClientAction.Escalate => ChatBotMessageNextActions.Escalate,
            ProblemDetailsClientAction.Dismiss => ChatBotMessageNextActions.Dismiss,
            ProblemDetailsClientAction.CorrectRequest => ChatBotMessageNextActions.CorrectRequest,
            _ => ChatBotMessageNextActions.None,
        };

    private static CommandGateway Gateway(
        ICommandDispatcher dispatcher,
        IAuthorizationStage? authorizationStage = null,
        IAuditWriter? auditWriter = null,
        IAuditReplayIntentQueue? replayQueue = null,
        IOperatorAlertSink? alertSink = null,
        ISystemClock? clock = null,
        IIdempotencyStore? idempotencyStore = null,
        ILifecycleTransitionGuard? lifecycleTransitionGuard = null,
        IChatBotProblemDetailsFactory? problemDetailsFactory = null,
        IOperationStatusStore? operationStatusStore = null,
        ISpineCommandAllowlist? commandAllowlist = null,
        IRiskClassifier? riskClassifier = null,
        IApprovalGate? approvalGate = null)
        => new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            authorizationStage ?? new PassThroughAuthorizationStage(),
            riskClassifier ?? new PassThroughRiskClassifier(),
            approvalGate ?? new PassThroughApprovalGate(),
            idempotencyStore ?? new InMemoryCoarseIdempotencyStore(clock ?? new FixedClock()),
            auditWriter ?? new RecordingAuditWriter(),
            replayQueue ?? new RecordingReplayIntentQueue(),
            alertSink ?? new RecordingOperatorAlertSink(),
            operationStatusStore ?? new InMemoryOperationStatusStore(),
            clock ?? new FixedClock(),
            lifecycleTransitionGuard ?? new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            problemDetailsFactory ?? DefaultProblemDetailsFactory(),
            commandAllowlist ?? new PermissiveSpineCommandAllowlist());

    private static IChatBotProblemDetailsFactory DefaultProblemDetailsFactory()
        => new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry());

    private static ChatBotCommandSubmission Submission(
        ClaimsPrincipal principal,
        object command,
        string? commandType = null,
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api)
        => new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType ?? command.GetType().Name,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            origin);

    private static Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake MailboxCommand(
        string intakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
        string mailboxId = "controlled-mailbox-001",
        string providerMessageId = "graph-message-001")
        => new(
            intakeId,
            new Hexalith.ChatBot.Contracts.Commands.MailboxMessageSourceIdentity(
                providerMessageId,
                "<message-001@example.test>",
                "graph-conversation-001",
                "graph-thread-001",
                mailboxId,
                new Hexalith.ChatBot.Contracts.Commands.MailboxParticipantIdentity("sender@example.test", "Sender"),
                new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.Zero),
                null,
                "UTC",
                "graph-message-v1",
                1),
            [new Hexalith.ChatBot.Contracts.Commands.MailboxRecipientIdentity("project@example.test", "Project", "to")],
            [new Hexalith.ChatBot.Contracts.Commands.MailboxAttachmentReference("attachment-001", "evidence.pdf", "application/pdf", 1024)]);

    private static Hexalith.ChatBot.Contracts.Commands.ScoreMailboxMessageAssociation AssociationScoringCommand(string kernelVersion)
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            "controlled-mailbox-001",
            "graph-conversation-001",
            "graph-thread-001",
            [new Hexalith.ChatBot.Contracts.Commands.AssociationDeterministicSignal(Hexalith.ChatBot.Contracts.Enums.AssociationSignalClass.ExplicitProjectIdentifier, "project-001", "mailbox:project-id", "hash-project", 0.9, true)],
            null,
            null,
            null,
            null,
            kernelVersion);

    private static Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject AssociationDecisionCommand(
        string commandNote = "Reviewed safe metadata.",
        string projectId = "project-001")
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            projectId,
            Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind.Associate,
            commandNote,
            "hash-project",
            1,
            "chatbot.association-decision-command.v1");

    private static Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation AssociationCorrectionCommand(
        string? rationale = "Wrong project selected from safe metadata.",
        string targetProjectId = "project-002")
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            "project-001",
            targetProjectId,
            Hexalith.ChatBot.Contracts.Enums.AssociationCorrectionKind.ProjectReassignment,
            rationale,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "hash-project-002",
            2,
            "chatbot.association-correction-command.v1");

    private static Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry RetryCommand(
        string reasonCode = "graph_throttled",
        string? note = "safe metadata retry")
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FC1",
            "01ARZ3NDEKTSV4RRFFQ69G5FC2",
            CoarseIdempotencyOperationClass.MessageIntake.Code,
            reasonCode,
            7,
            note);

    private static Hexalith.ChatBot.Contracts.Commands.ProposeAIAction AiProposalCommand(
        string intendedCommandName = "Project.AppendConversationMessage")
        => new(
            "project-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            intendedCommandName,
            "project-conversation",
            8,
            ["message:offset:001"],
            ["project:project-001"],
            [],
            "tenant-alpha:policy:ai-action-risk",
            CorrelationId,
            "transition-001",
            SourceConversationItemId: "conversation-item-001");

    private static Hexalith.ChatBot.Contracts.Commands.ExecuteLowRiskAIAssistance LowRiskExecutionCommand(string executionId = "ai-execution-001")
        => new(
            "project-001",
            "ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            Hexalith.ChatBot.Contracts.Queries.LowRiskAiAssistanceKind.SummarizeVisibleContext,
            "context-package-001",
            "v1",
            "metadata_only",
            "collaboration_input",
            "disabled",
            ["evidence-001"],
            ["evidence-001"],
            ["redacted"],
            8,
            "policy-snap-001",
            CorrelationId,
            executionId,
            "transition-001",
            SourceConversationItemId: "conversation-item-001");

    private static Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval ApprovalDecisionCommand(
        string decisionId = "approval-decision-001",
        Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind decision = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind.Approve)
        => new(
            "project-001",
            "approval:ai-proposal-001",
            "ai-proposal-001",
            "graph-message-001",
            decision,
            9,
            CorrelationId,
            decisionId);

    private static ClaimsPrincipal Principal(string? tenantId, params Claim[] additionalClaims)
    {
        List<Claim> claims = [new("sub", ActorId)];
        if (tenantId is not null)
        {
            claims.Add(new Claim("eventstore:tenant", tenantId));
        }

        claims.AddRange(additionalClaims);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static ClaimsPrincipal PrincipalWithTenantClaims(params string[] tenantIds)
    {
        List<Claim> claims = [new("sub", ActorId)];
        claims.AddRange(tenantIds.Select(static tenantId => new Claim("eventstore:tenant", tenantId)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static string Serialized(ProblemDetails problem)
        => JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private sealed record TenantScopedCommand(string TenantId, string ResourceName);

    // Default allowlist for the stage tests, which exercise admission/audit/idempotency/lifecycle paths
    // with a generic command. Allowlist enforcement itself is covered by the dedicated allowlist tests
    // that inject the real hardcoded ChatBotSpineCommandAllowlist.
    private sealed class PermissiveSpineCommandAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => true;
    }

    private sealed class FixedCorrectionDependencyReadiness(bool ready) : IAssociationCorrectionDependencyReadiness
    {
        public AssociationCorrectionDependencyReadinessStatus Status { get; } = new(ready, ready, ready, ready);

        public bool IsProjectionInvalidationReady => ready;
    }

    private sealed class FixedTenantAiPolicySnapshotProvider(bool lowRiskAllowed) : ITenantAiPolicySnapshotProvider
    {
        public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
            string tenantId,
            string projectId,
            string? requestedPolicySnapshotId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<TenantAiPolicySnapshot?>(new TenantAiPolicySnapshot(
                requestedPolicySnapshotId ?? "policy-snap-001",
                lowRiskAllowed,
                "read-only",
                ["summarize-visible-context"],
                IsFresh: true,
                IsValid: true));
    }

    private sealed record TenantScopedIdentifierCommand(string ProjectId);

    private sealed class RecordingAuthenticationStage(List<string> stages) : IAuthenticationStage
    {
        public ValueTask<ChatBotAuthenticationResult> AuthenticateAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
        {
            stages.Add("auth");
            return ValueTask.FromResult(ChatBotAuthenticationResult.Authenticated(ActorId, submission.Principal));
        }
    }

    private sealed class RecordingTenantBindingStage(List<string> stages) : ITenantBindingStage
    {
        public ValueTask<ChatBotTenantBindingResult> BindTenantAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            CancellationToken cancellationToken)
        {
            stages.Add("tenant-bind");
            return ValueTask.FromResult(ChatBotTenantBindingResult.Bound(new ChatBotTenantBinding(BoundTenant)));
        }
    }

    private sealed class RecordingAuthorizationStage(List<string> stages, ChatBotAuthorizationResult result) : IAuthorizationStage
    {
        public ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
        {
            stages.Add("authorize");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingRiskClassifier(List<string> stages) : IRiskClassifier
    {
        public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages.Add("risk-classify");
            return ValueTask.FromResult(ChatBotRiskClassification.PassThrough);
        }
    }

    private sealed class OriginTamperingRiskClassifier : IRiskClassifier
    {
        public ChatBotSurfaceOrigin OriginObservedAfterTamperAttempt { get; private set; }

        public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            // Attempt to rewrite the immutable surface origin from inside the pipeline. `with` yields a separate
            // copy that is discarded; the gateway's submission is untouched.
            _ = context.Submission with { Origin = ChatBotSurfaceOrigin.Cli };
            OriginObservedAfterTamperAttempt = context.Submission.Origin;
            return ValueTask.FromResult(ChatBotRiskClassification.PassThrough);
        }
    }

    private sealed class RecordingApprovalGate(List<string> stages) : IApprovalGate
    {
        public ValueTask<ChatBotApprovalResult> EvaluateAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages.Add("approval-gate");
            return ValueTask.FromResult(ChatBotApprovalResult.Approved);
        }
    }

    private sealed class RecordingIdempotencyStore(List<string>? stages = null) : IIdempotencyStore
    {
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            stages?.Add("coarse-idempotency");
            CoarseIdempotencyMetadata metadata = CoarseIdempotencyMetadata.UnsafeCreateForTesting(
                "command-execution",
                "recording-key",
                "recording-equivalence",
                DateTimeOffset.UtcNow.AddSeconds(60));
            context.SetIdempotency(metadata);
            return ValueTask.FromResult(CoarseIdempotencyDecision.Proceed(metadata));
        }

        public ValueTask RecordOutcomeAsync(
            CoarseIdempotencyMetadata metadata,
            CommandSubmissionResponse outcome,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class FixedLifecycleTransitionGuard(LifecycleTransitionValidation result) : ILifecycleTransitionGuard
    {
        public LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context)
            => result;
    }

    private sealed class RecordingLifecycleTransitionGuard(List<string> stages) : ILifecycleTransitionGuard
    {
        public LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context)
        {
            stages.Add("lifecycle-validation");
            return LifecycleTransitionValidation.Valid(new LifecycleTransitionDefinition("Received", "Proposed"));
        }
    }

    private sealed class ConflictingIdempotencyStore : IIdempotencyStore
    {
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            CoarseIdempotencyMetadata metadata = CoarseIdempotencyMetadata.UnsafeCreateForTesting(
                "command-execution",
                "conflict-key",
                "conflict-equivalence",
                DateTimeOffset.UtcNow.AddSeconds(60));
            context.SetIdempotency(metadata);
            return ValueTask.FromResult(CoarseIdempotencyDecision.Conflict(metadata));
        }

        public ValueTask RecordOutcomeAsync(
            CoarseIdempotencyMetadata metadata,
            CommandSubmissionResponse outcome,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class RecordingAuditWriter(List<string>? stages = null) : IAuditWriter
    {
        public List<ChatBotAuthorizationFailureAuditFact> AuthorizationFailures { get; } = [];
        public List<AuditEnvelope> Envelopes { get; } = [];
        public AuditWriteResult PreCommitResult { get; init; } = AuditWriteResult.Success;
        public AuditWriteResult PostCommitResult { get; init; } = AuditWriteResult.Success;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
        {
            AuthorizationFailures.Add(fact);
            return ValueTask.CompletedTask;
        }

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            stages?.Add("pre-commit-audit");
            return ValueTask.FromResult(PreCommitResult);
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            stages?.Add("post-commit-audit");
            return ValueTask.FromResult(PostCommitResult);
        }
    }

    private sealed class RecordingReplayIntentQueue : IAuditReplayIntentQueue
    {
        public List<AuditReplayIntent> Intents { get; } = [];

        public ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
        {
            Intents.Add(intent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingOperatorAlertSink : IOperatorAlertSink
    {
        public List<OperatorAlert> Alerts { get; } = [];

        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
        {
            Alerts.Add(alert);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public static DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 30, 18, 30, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => FixedUtcNow;
    }

    private sealed class RecordingDispatcher(List<string>? stages = null) : ICommandDispatcher
    {
        public int DispatchCount { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            DispatchCount++;
            stages?.Add("dispatch");
            return ValueTask.FromResult(new ChatBotDispatchResult(DateTimeOffset.UtcNow));
        }
    }

    private sealed class ThrowingDispatcher : ICommandDispatcher
    {
        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
            => throw new HttpRequestException("EventStore gateway is unreachable.");
    }

    private sealed class AbortTrackingIdempotencyStore : IIdempotencyStore
    {
        public int AbortCount { get; private set; }

        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            CoarseIdempotencyMetadata metadata = CoarseIdempotencyMetadata.UnsafeCreateForTesting(
                "command-execution",
                "abort-tracking-key",
                "abort-tracking-equivalence",
                DateTimeOffset.UtcNow.AddSeconds(60));
            context.SetIdempotency(metadata);
            return ValueTask.FromResult(CoarseIdempotencyDecision.Proceed(metadata));
        }

        public ValueTask RecordOutcomeAsync(
            CoarseIdempotencyMetadata metadata,
            CommandSubmissionResponse outcome,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
        {
            AbortCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyingAuthorizationStage(string reasonCode) : IAuthorizationStage
    {
        public ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ChatBotAuthorizationResult.Denied(reasonCode));
    }
}
