using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1.DerivedStores;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// Production adapter for Memories' durable canonical correction workflow. It never writes canonical backend records
/// directly and reports nonterminal evidence so the enclosing Dapr workflow can poll with durable timers.
/// </summary>
internal sealed class MemoriesVectorReindexer(MemoriesClient client, ISystemClock clock)
    : IVectorReindexer, ICanonicalVectorReindexer
{
    public const string RemoteFailureReasonCode = "memories_correction_failed";
    public const string InvalidStatusReasonCode = "memories_correction_invalid_status";
    public const string TerminalTimeoutReasonCode = "memories_correction_timed_out";

    public ValueTask<VectorReindexOutcome> ReindexVectorsAsync(
        string tenantId,
        string correctionId,
        long sourceVersion,
        IReadOnlyList<string> affectedResourceIds,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(Failed(InvalidStatusReasonCode, CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M2, startedAtUtc)));

    public async ValueTask<VectorReindexOutcome> ReindexCanonicalVectorsAsync(
        string tenantId,
        string associationId,
        string intakeId,
        string correctionId,
        long sourceVersion,
        string correctedCaseId,
        string? remoteOperationId,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correctionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correctedCaseId);

        DateTimeOffset fallbackDeadline = CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M2, startedAtUtc);
        try
        {
            StartDerivedStoreCorrectionRequest request = new(
                associationId,
                intakeId,
                correctionId,
                sourceVersion,
                correctedCaseId);
            DerivedStoreCorrectionStatus status = string.IsNullOrWhiteSpace(remoteOperationId)
                ? await client
                    .StartOrRejoinDerivedStoreCorrectionAsync(tenantId, request, cancellationToken)
                    .ConfigureAwait(false)
                : await client
                    .GetDerivedStoreCorrectionStatusAsync(tenantId, remoteOperationId, cancellationToken)
                    .ConfigureAwait(false);

            if (!Matches(status, request))
            {
                return Failed(InvalidStatusReasonCode, fallbackDeadline);
            }

            DateTimeOffset completedAt = status.CompletedAtUtc ?? clock.UtcNow;
            return status.State switch
            {
                DerivedStoreCorrectionState.Pending or DerivedStoreCorrectionState.Running => new VectorReindexOutcome(
                    status.EntriesInvalidated,
                    status.EntriesRebuilt,
                    status.VersionGuardSkipped,
                    SloBreached: false,
                    status.DeadlineUtc,
                    completedAt,
                    FailureReasonCode: null,
                    IsTerminal: false,
                    RemoteOperationId: status.OperationId),
                DerivedStoreCorrectionState.Succeeded => new VectorReindexOutcome(
                    status.EntriesInvalidated,
                    status.EntriesRebuilt,
                    status.VersionGuardSkipped,
                    completedAt > status.DeadlineUtc,
                    status.DeadlineUtc,
                    completedAt,
                    FailureReasonCode: null,
                    RemoteOperationId: status.OperationId),
                DerivedStoreCorrectionState.NoOp => new VectorReindexOutcome(
                    status.EntriesInvalidated,
                    status.EntriesRebuilt,
                    VersionGuardSkipped: true,
                    completedAt > status.DeadlineUtc,
                    status.DeadlineUtc,
                    completedAt,
                    FailureReasonCode: null,
                    RemoteOperationId: status.OperationId),
                DerivedStoreCorrectionState.TimedOut => new VectorReindexOutcome(
                    status.EntriesInvalidated,
                    status.EntriesRebuilt,
                    status.VersionGuardSkipped,
                    SloBreached: true,
                    status.DeadlineUtc,
                    completedAt,
                    TerminalTimeoutReasonCode,
                    RemoteOperationId: status.OperationId),
                DerivedStoreCorrectionState.Failed => new VectorReindexOutcome(
                    status.EntriesInvalidated,
                    status.EntriesRebuilt,
                    status.VersionGuardSkipped,
                    completedAt > status.DeadlineUtc,
                    status.DeadlineUtc,
                    completedAt,
                    AuditMetadata.SafeOptionalToken(status.FailureReasonCode) ?? RemoteFailureReasonCode,
                    RemoteOperationId: status.OperationId),
                _ => Failed(InvalidStatusReasonCode, fallbackDeadline),
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return Failed(RemoteFailureReasonCode, fallbackDeadline);
        }
    }

    private VectorReindexOutcome Failed(string reasonCode, DateTimeOffset deadline)
        => new(0, 0, false, clock.UtcNow > deadline, deadline, clock.UtcNow, reasonCode);

    private static bool Matches(DerivedStoreCorrectionStatus status, StartDerivedStoreCorrectionRequest request)
        => !string.IsNullOrWhiteSpace(status.OperationId)
        && string.Equals(status.AssociationId, request.AssociationId, StringComparison.Ordinal)
        && string.Equals(status.IntakeId, request.IntakeId, StringComparison.Ordinal)
        && string.Equals(status.CorrectionId, request.CorrectionId, StringComparison.Ordinal)
        && status.SourceVersion == request.SourceVersion
        && string.Equals(status.CorrectedCaseId, request.CorrectedCaseId, StringComparison.Ordinal);
}
