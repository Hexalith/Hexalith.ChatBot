using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.UI.State.GovernedOperations;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// The UI's single seam onto the governed command spine. It submits the trivial <see cref="RecordGovernedNote"/>
/// command and reads the resulting operation status <b>only</b> through <see cref="IChatBotClient"/>, declaring
/// the <see cref="ChatBotSurfaceOrigin.Ui"/> surface origin at the boundary. It never touches Server internals,
/// the gateway stages, or the audit/idempotency seams — those are encapsulated behind the client facade.
/// </summary>
public sealed class GovernedOperationService(IChatBotClient client)
{
    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>
    /// Submits a governed note through the spine and reads back its metadata-only outcome.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The metadata-only operation outcome.</returns>
    public async Task<OperationOutcome> SubmitGovernedNoteAsync(CancellationToken cancellationToken = default)
    {
        RecordGovernedNote command = new(GovernedNoteId.New().Value);

        CommandSubmissionResponse response = await _client
            .SubmitAsync(command, origin: ChatBotSurfaceOrigin.Ui, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // TaskId is optional and may arrive as an empty string rather than null; `??` would then yield an empty
        // operation id that is used for two spine reads and stamped into the UI as the operation identity.
        string operationId = string.IsNullOrWhiteSpace(response.TaskId) ? response.CommandId : response.TaskId;
        OperationStatus status = await _client
            .GetOperationStatusAsync(operationId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Audit history is a REAL tenant-scoped, redacted, metadata-only read of the operation's post-commit
        // audit envelope summary through the spine (Story 1.9 M3) — not a client-side fabrication. The richer
        // governed audit-query/investigation surface is deferred (Epic 9 / Story 9.3).
        OperationAuditHistory auditHistory = await _client
            .GetOperationAuditHistoryAsync(operationId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new OperationOutcome(
            operationId,
            response.CommandId,
            response.CorrelationId,
            status.LifecycleState.ToString(),
            status.CompletionStatus.ToString(),
            status.AuditStatus.ToString(),
            [.. status.SafeNextActions.Select(static action => action.ToString())],
            ToAuditHistoryLines(auditHistory),
            status.RetryCount,
            status.OperationClass,
            status.OwnerRole,
            status.DuplicateSafetyNote);
    }

    // Renders the server's post-commit audit envelope summary into metadata-only display lines: stable codes and
    // opaque correlation tokens only — never the command payload, tenant/resource names, secrets, or raw text.
    private static IReadOnlyList<string> ToAuditHistoryLines(OperationAuditHistory auditHistory)
        => auditHistory.Entries.Count == 0
            ? [$"audit:{auditHistory.AuditStatus} · awaiting post-commit record"]
            :
            [
                .. auditHistory.Entries.Select(entry =>
                    $"{entry.Phase} · {entry.Decision}/{entry.Outcome} · audit:{auditHistory.AuditStatus} · origin:{entry.SurfaceOrigin} · correlation:{entry.CorrelationId}"),
            ];
}
