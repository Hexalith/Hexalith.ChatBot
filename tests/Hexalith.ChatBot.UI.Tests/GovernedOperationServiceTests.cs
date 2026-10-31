using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.GovernedOperations;

using Shouldly;

using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Covers the UI's single seam onto the governed spine (AC1 + UX floor): it submits the trivial governed
/// command declaring the <c>ui</c> surface origin, reads the outcome back through the operation status, and
/// renders a freshness-honest, metadata-only view — never a premature "Done".
/// </summary>
public sealed class GovernedOperationServiceTests
{
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task SubmitGovernedNoteShouldDeclareTheUiSurfaceOriginAtTheBoundary()
    {
        FakeChatBotClient client = new(Response(), Status());
        GovernedOperationService service = new(client);

        _ = await service.SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        // The UI is a first-party adapter: it must declare 'ui' so the origin travels into the audit envelope.
        client.LastOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        client.LastSubmittedCommand.ShouldBeOfType<RecordGovernedNote>();
    }

    [Fact]
    public async Task SubmitGovernedNoteShouldReadOutcomeBackThroughOperationStatusKeyedByAcceptedOperationId()
    {
        FakeChatBotClient client = new(Response(), Status());
        GovernedOperationService service = new(client);

        OperationOutcome outcome = await service.SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        // The accepted operation identity differs from TaskId and keys both typed status and audit reads.
        client.LastOperationId.ShouldBe(CommandId);
        outcome.OperationId.ShouldBe(CommandId);
        outcome.CommandId.ShouldBe(CommandId);
        outcome.CorrelationId.ShouldBe(CorrelationId);
        outcome.LifecycleState.ShouldBe(nameof(LifecycleState.Proposed));
        outcome.AuditStatus.ShouldBe(nameof(OperationAuditStatus.Committed));
        outcome.SafeNextActions.ShouldBe([nameof(ChatBotMessageNextAction.None)]);
        outcome.RetryCount.ShouldBe(0);
        outcome.OperationClass.ShouldBe("message-intake");
        outcome.OwnerRole.ShouldBe("mailbox-operator");
        outcome.DuplicateSafetyNote.ShouldBe("duplicate-safe");
    }

    [Fact]
    public async Task SubmitGovernedNoteShouldSurfaceFreshnessHonestCompletionAndNeverAPrematureDone()
    {
        FakeChatBotClient client = new(
            Response(),
            Status(completionStatus: OperationCompletionStatus.AcceptedProjectionPending));
        GovernedOperationService service = new(client);

        OperationOutcome outcome = await service.SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        outcome.CompletionStatus.ShouldBe(nameof(OperationCompletionStatus.AcceptedProjectionPending));
        outcome.CompletionStatus.ShouldNotBe(nameof(OperationCompletionStatus.Completed));
    }

    [Fact]
    public async Task SubmitGovernedNoteShouldFallBackToTheCommandIdWhenNoTaskIdIsReturned()
    {
        FakeChatBotClient client = new(Response(taskId: null, operationId: null), Status());
        GovernedOperationService service = new(client);

        OperationOutcome outcome = await service.SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        client.LastOperationId.ShouldBe(CommandId);
        outcome.OperationId.ShouldBe(CommandId);
    }

    [Fact]
    public async Task SubmitGovernedNoteShouldExposeMetadataOnlyAuditHistoryReadThroughTheSpine()
    {
        FakeChatBotClient client = new(Response(), Status());
        GovernedOperationService service = new(client);

        OperationOutcome outcome = await service.SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        // Audit history is a REAL server read of the post-commit audit envelope summary (Story 1.9 M3), keyed by
        // the same operation id and mapped to structured metadata: stable codes and opaque identifiers only.
        // GovernedAuditPresentationRenderTests.GovernedAuditShouldWithholdOmittedTransportFieldsThroughServiceAndPage
        // proves that sentinels seeded into the history operation id, ResourceId, ReasonCode, StateTransition and
        // RecordedAt reach neither the serialized service model nor the rendered page.
        client.LastAuditHistoryOperationId.ShouldBe(CommandId);
        outcome.AuditHistory.AuditStatus.ShouldBe(OperationAuditStatus.Committed);
        OperationAuditEntryModel entry = outcome.AuditHistory.Entries.ShouldHaveSingleItem();
        entry.Phase.ShouldBe(AuditHistoryPhase.PostCommit);
        entry.Decision.ShouldBe("allow");
        entry.Outcome.ShouldBe("proposed");
        entry.SurfaceOrigin.ShouldBe(SurfaceOrigin.Ui);
        entry.CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public async Task EmptyAuditHistoryShouldPreserveReadStatusWithoutInventingAnEntry()
    {
        OperationAuditHistory history = new() { AuditStatus = OperationAuditStatus.Reconciling, Entries = [] };
        FakeChatBotClient client = new(Response(), Status(), history);

        OperationOutcome outcome = await new GovernedOperationService(client)
            .SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        outcome.AuditStatus.ShouldBe(nameof(OperationAuditStatus.Committed));
        outcome.AuditHistory.AuditStatus.ShouldBe(OperationAuditStatus.Reconciling);
        outcome.AuditHistory.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HistoricalReceiptShouldRetainTaskIdentityWhenOperationIdentityIsAbsent(string? operationId)
    {
        FakeChatBotClient client = new(Response(operationId: operationId), Status());
        OperationOutcome outcome = await new GovernedOperationService(client).SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);
        client.LastOperationId.ShouldBe(TaskId);
        client.LastAuditHistoryOperationId.ShouldBe(TaskId);
        outcome.OperationId.ShouldBe(TaskId);
    }

    private static CommandSubmissionResponse Response(string? taskId = TaskId, string? operationId = CommandId)
        => new()
        {
            CommandId = CommandId,
            OperationId = operationId!,
            CorrelationId = CorrelationId,
            TaskId = taskId,
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
        };

    private static OperationStatus Status(
        OperationCompletionStatus completionStatus = OperationCompletionStatus.AcceptedProjectionPending,
        OperationAuditStatus auditStatus = OperationAuditStatus.Committed)
    {
        DateTimeOffset at = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);
        return new OperationStatus
        {
            OperationId = TaskId,
            CommandId = CommandId,
            CorrelationId = CorrelationId,
            LifecycleState = LifecycleState.Proposed,
            RetryCount = 0,
            CompletionStatus = completionStatus,
            AuditStatus = auditStatus,
            PartialOutputs = new OperationStatusPartialOutputs
            {
                AcceptedAt = at,
                CompletionStatus = completionStatus,
                AuditStatus = auditStatus,
            },
            SafeNextActions = [ChatBotMessageNextAction.None],
            OperationClass = "message-intake",
            MaxAttempts = 5,
            DuplicateSafetyNote = "duplicate-safe",
            OwnerRole = "mailbox-operator",
            AcceptedAt = at,
            LastUpdatedAt = at,
        };
    }

    private sealed class FakeChatBotClient(CommandSubmissionResponse response, OperationStatus status, OperationAuditHistory? history = null) : IChatBotClient
    {
        public ChatBotSurfaceOrigin? LastOrigin { get; private set; }

        public string? LastOperationId { get; private set; }

        public string? LastAuditHistoryOperationId { get; private set; }

        public IChatBotCommand? LastSubmittedCommand { get; private set; }

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            LastSubmittedCommand = command;
            LastOrigin = origin;
            return Task.FromResult(response);
        }

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            LastOperationId = operationId;
            return Task.FromResult(status);
        }

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            LastAuditHistoryOperationId = operationId;
            return Task.FromResult(history ?? new OperationAuditHistory
            {
                OperationId = operationId,
                AuditStatus = OperationAuditStatus.Committed,
                Entries =
                [
                    new AuditHistoryEntry
                    {
                        Phase = AuditHistoryPhase.PostCommit,
                        Decision = "allow",
                        ReasonCode = "eventstore_dispatch_accepted",
                        Outcome = "proposed",
                        StateTransition = "Received->Proposed",
                        RedactionDecision = AuditHistoryEntryRedactionDecision.Metadata_only,
                        SurfaceOrigin = SurfaceOrigin.Ui,
                        ResourceId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                        CorrelationId = CorrelationId,
                        RecordedAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
                    },
                ],
            });
        }

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
