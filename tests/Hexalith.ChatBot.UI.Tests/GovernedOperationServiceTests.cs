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
    public async Task SubmitGovernedNoteShouldReadOutcomeBackThroughOperationStatusKeyedByTaskId()
    {
        FakeChatBotClient client = new(Response(), Status());
        GovernedOperationService service = new(client);

        OperationOutcome outcome = await service.SubmitGovernedNoteAsync(TestContext.Current.CancellationToken);

        // The operation read is keyed by the task id when present, and the outcome reflects the status fields.
        client.LastOperationId.ShouldBe(TaskId);
        outcome.OperationId.ShouldBe(TaskId);
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
        FakeChatBotClient client = new(Response(taskId: null), Status());
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
        // the same operation id and rendered as a redacted, metadata-only line: stable codes and opaque
        // identifiers only — no command payload, tenant/resource names, secrets, paths, or raw exception text.
        client.LastAuditHistoryOperationId.ShouldBe(TaskId);
        outcome.AuditHistory.ShouldNotBeEmpty();
        outcome.AuditHistory.ShouldContain(line =>
            line.Contains($"audit:{OperationAuditStatus.Committed}", StringComparison.Ordinal)
            && line.Contains($"origin:{SurfaceOrigin.Ui}", StringComparison.Ordinal)
            && line.Contains($"correlation:{CorrelationId}", StringComparison.Ordinal));

        foreach (string line in outcome.AuditHistory)
        {
            line.ShouldNotContain("noteId", Case.Insensitive);
            line.ShouldNotContain("tenant", Case.Insensitive);
            line.ShouldNotContain("tenant-alpha", Case.Insensitive);
            line.ShouldNotContain("resource", Case.Insensitive);
            line.ShouldNotContain("restricted-file.txt", Case.Insensitive);
            line.ShouldNotContain("Secret Project", Case.Insensitive);
            line.ShouldNotContain("payload", Case.Insensitive);
            line.ShouldNotContain("secret", Case.Insensitive);
            line.ShouldNotContain("/home/", Case.Insensitive);
            line.ShouldNotContain("exception", Case.Insensitive);
        }
    }

    private static CommandSubmissionResponse Response(string? taskId = TaskId)
        => new()
        {
            CommandId = CommandId,
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

    private sealed class FakeChatBotClient(CommandSubmissionResponse response, OperationStatus status) : IChatBotClient
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
            return Task.FromResult(new OperationAuditHistory
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
