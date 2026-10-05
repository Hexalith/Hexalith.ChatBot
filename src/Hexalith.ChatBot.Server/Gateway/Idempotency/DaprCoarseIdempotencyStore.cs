using System.Collections.Concurrent;

using Dapr.Client;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class DaprCoarseIdempotencyStore : IIdempotencyStore
{
    private readonly ICoarseIdempotencyStateClient _state;
    private readonly ISystemClock clock;
    private readonly IAuditHistoryReader? _auditHistory;
    private readonly ConcurrentDictionary<string, CommandSubmissionResponse> _pendingOutcomes = new(StringComparer.Ordinal);

    public DaprCoarseIdempotencyStore(DaprClient client, ISystemClock clock, IAuditHistoryReader auditHistory)
        : this(new DaprCoarseIdempotencyStateClient(client), clock, auditHistory)
    {
    }

    internal DaprCoarseIdempotencyStore(ICoarseIdempotencyStateClient state, ISystemClock clock, IAuditHistoryReader? auditHistory = null)
    {
        _state = state;
        this.clock = clock;
        _auditHistory = auditHistory;
    }

    public async ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        DateTimeOffset now = clock.UtcNow;
        CoarseIdempotencyRecord proposed = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, now)
            with { ReservationId = Guid.NewGuid().ToString("N") };
        CoarseIdempotencyMetadata metadata = Metadata(proposed);
        context.SetIdempotency(metadata);

        CoarseCommandIdentityRecord? owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
        if (owner is not null)
        {
            if (string.Equals(owner.CallerFingerprint, proposed.CallerFingerprint, StringComparison.Ordinal))
            {
                await RecoverPendingOutcomeAsync(proposed.IdentityKeyHash!, owner, cancellationToken).ConfigureAwait(false);
                owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false) ?? owner;
            }

            return await IdentityDecisionAsync(owner, proposed, metadata, cancellationToken).ConfigureAwait(false);
        }

        // Old generic records used a body-derived key and had no caller-ID index. Keep their
        // unexpired, unchanged-body replay path during the upgrade window.
        if (proposed.LegacyKeyHash is { } legacyKey)
        {
            (CoarseIdempotencyRecord? legacy, _) = await ReadDomainAsync(legacyKey, cancellationToken).ConfigureAwait(false);
            if (legacy is not null && legacy.ExpiresAt > now &&
                string.Equals(legacy.CommandId, proposed.CommandId, StringComparison.Ordinal))
            {
                return legacy.PriorOutcome is null
                    ? CoarseIdempotencyDecision.RecoveryPending(metadata)
                    : await ClaimReplayIdentityAsync(legacy, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }
        }

        string? receiptKey = DomainReceiptKey(proposed.OperationClass, proposed.CoarseKeyHash);
        if (receiptKey is not null)
        {
            (CoarseIdempotencyRecord? receipt, string receiptEtag) = await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
            if (receipt is not null)
            {
                if (receipt.PriorOutcome is not null && receipt.ExpiresAt <= now)
                {
                    (CoarseIdempotencyRecord? primary, _) = await ReadDomainAsync(proposed.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
                    if (primary is not null && primary.PriorOutcome is null && SameReservation(primary, receipt))
                    {
                        return await DomainDecisionAsync(receipt, proposed, metadata, cancellationToken).ConfigureAwait(false);
                    }

                    if (!await _state.TryDeleteAsync(receiptKey, receiptEtag, cancellationToken).ConfigureAwait(false))
                    {
                        return CoarseIdempotencyDecision.RecoveryPending(metadata);
                    }
                }
                else
                {
                    return await DomainDecisionAsync(receipt, proposed, metadata, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        (CoarseIdempotencyRecord? existing, string etag) = await ReadDomainAsync(proposed.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.ExpiresAt <= now)
        {
            if (existing.PriorOutcome is null)
            {
                return CoarseIdempotencyDecision.Conflict(metadata);
            }

            if (!await EnsureIdentityOutcomeAsync(existing, cancellationToken).ConfigureAwait(false))
            {
                return CoarseIdempotencyDecision.Conflict(metadata);
            }

            if (!await _state.TryDeleteAsync(proposed.CoarseKeyHash, etag ?? string.Empty, cancellationToken).ConfigureAwait(false))
            {
                return CoarseIdempotencyDecision.Conflict(metadata);
            }
            existing = null;
            etag = string.Empty;
        }

        if (existing is not null)
        {
            return await DomainDecisionAsync(existing, proposed, metadata, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            bool saved = await _state.TrySaveDomainAsync(proposed.CoarseKeyHash, proposed, etag ?? string.Empty, cancellationToken).ConfigureAwait(false);
            if (!saved)
            {
                (existing, _) = await ReadDomainAsync(proposed.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
                return existing is null ? CoarseIdempotencyDecision.Conflict(metadata) :
                    await DomainDecisionAsync(existing, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }

            if (receiptKey is not null &&
                !await _state.TrySaveDomainAsync(receiptKey, proposed, string.Empty, cancellationToken).ConfigureAwait(false))
            {
                await DeletePendingDomainAsync(proposed.CoarseKeyHash, proposed, cancellationToken).ConfigureAwait(false);
                (CoarseIdempotencyRecord? currentReceipt, _) = await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
                return currentReceipt is null ? CoarseIdempotencyDecision.Conflict(metadata) :
                    await DomainDecisionAsync(currentReceipt, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }

            CoarseCommandIdentityRecord identity = new(
                proposed.TenantId, proposed.CommandId, proposed.CallerFingerprint!, proposed.CoarseKeyHash, now, null, proposed);
            bool claimed = await _state.TrySaveIdentityAsync(proposed.IdentityKeyHash!, identity, string.Empty, cancellationToken).ConfigureAwait(false);
            if (claimed)
            {
                return CoarseIdempotencyDecision.Proceed(metadata);
            }

            owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
            if (receiptKey is not null)
            {
                await DeletePendingDomainAsync(receiptKey, proposed, cancellationToken).ConfigureAwait(false);
            }
            await DeletePendingDomainAsync(proposed.CoarseKeyHash, proposed, cancellationToken).ConfigureAwait(false);
            return owner is null ? CoarseIdempotencyDecision.Conflict(metadata) :
                await IdentityDecisionAsync(owner, proposed, metadata, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await DeleteOwnedPendingIdentityAsync(proposed.IdentityKeyHash!, proposed, cancellationToken).ConfigureAwait(false);
            if (receiptKey is not null)
            {
                await DeletePendingDomainAsync(receiptKey, proposed, cancellationToken).ConfigureAwait(false);
            }
            await DeletePendingDomainAsync(proposed.CoarseKeyHash, proposed, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask RecordOutcomeAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(outcome);
        CommandSubmissionResponse stored = Clone(outcome);
        string recoveryKey = metadata.IdentityKeyHash ?? metadata.CoarseKeyHash;
        _pendingOutcomes[recoveryKey] = Clone(stored);
        string? receiptKey = DomainReceiptKey(metadata.OperationClass, metadata.CoarseKeyHash);
        bool identityStored = false;
        bool domainStored = false;
        bool receiptStored = receiptKey is null;
        Exception? firstFailure = null;

        // Each receipt is attempted independently. A thrown state call after dispatch must
        // not prevent another durable record from keeping the committed outcome recoverable.
        try
        {
            identityStored = await EnsureIdentityOutcomeAsync(metadata.IdentityKeyHash, metadata.CoarseKeyHash, stored, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            firstFailure = exception;
        }

        if (receiptKey is not null)
        {
            try
            {
                receiptStored = await EnsureDomainReceiptAsync(receiptKey, metadata, stored, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                firstFailure ??= exception;
            }
        }

        try
        {
            domainStored = await EnsurePrimaryDomainOutcomeAsync(metadata.CoarseKeyHash, stored, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            firstFailure ??= exception;
        }

        // A specialized command needs a domain-level receipt. Its caller identity alone
        // cannot protect an equivalent retry with a different caller command ID.
        if (!receiptStored || (receiptKey is null && !identityStored && !domainStored))
        {
            throw new InvalidOperationException("The admitted outcome has no durable domain receipt.", firstFailure);
        }

        _pendingOutcomes.TryRemove(recoveryKey, out _);
    }

    public async ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadata.IdentityKeyHash is { } identityKey)
        {
            (CoarseCommandIdentityRecord? identity, string identityEtag) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
            if (identity is not null && identity.DomainKeyHash == metadata.CoarseKeyHash &&
                identity.PriorOutcome is null &&
                (metadata.CreatedAt is null || identity.CreatedAt == metadata.CreatedAt) &&
                string.Equals(identity.DomainReservation?.ReservationId, metadata.ReservationId, StringComparison.Ordinal))
            {
                await DeletePendingAsync(identityKey, identityEtag, identity, null, cancellationToken).ConfigureAwait(false);
            }
        }

        if (DomainReceiptKey(metadata.OperationClass, metadata.CoarseKeyHash) is { } receiptKey)
        {
            await DeletePendingDomainForMetadataAsync(receiptKey, metadata, cancellationToken).ConfigureAwait(false);
        }

        await DeletePendingDomainForMetadataAsync(metadata.CoarseKeyHash, metadata, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reconciles a post-dispatch, metadata-only receipt from the independent audit replay queue.</summary>
    internal async ValueTask<bool> ReconcileOutcomeAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (intent.AcceptedOutcome is not { } outcome || intent.IdentityKeyHash is not { } identityKey ||
            intent.CoarseKeyHash is not { } domainKey ||
            intent.Kind != AuditReplayIntentKind.PostCommitAuditReconciliation)
        {
            return false;
        }

        CoarseCommandIdentityRecord? identity = await ReadIdentityAsync(identityKey, cancellationToken).ConfigureAwait(false);
        if (identity?.DomainReservation is not { } reservation ||
            !string.Equals(identity.TenantId, intent.TenantId, StringComparison.Ordinal) ||
            !string.Equals(identity.CommandId, outcome.CommandId, StringComparison.Ordinal) ||
            !string.Equals(identity.DomainKeyHash, domainKey, StringComparison.Ordinal) ||
            !string.Equals(reservation.IdentityKeyHash, identityKey, StringComparison.Ordinal) ||
            !string.Equals(reservation.CorrelationId, outcome.CorrelationId, StringComparison.Ordinal) ||
            !string.Equals(outcome.OperationId, outcome.TaskId ?? outcome.CommandId, StringComparison.Ordinal))
        {
            return false;
        }

        await RecordOutcomeAsync(Metadata(reservation), outcome, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>Restores a receipt from a retained post-commit audit envelope after a process restart.</summary>
    internal async ValueTask<bool> ReconcileOutcomeFromAuditAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Phase != AuditCommitPhase.PostCommit ||
            !string.Equals(envelope.ReasonCode, "eventstore_dispatch_accepted", StringComparison.Ordinal) ||
            envelope.IdempotencyKey is not { } domainKey ||
            SingleEvidenceValue(envelope, "command:") is not { } commandId ||
            SingleEvidenceValue(envelope, "operation:") is not { } operationId ||
            SingleEvidenceValue(envelope, "identity-key:") is not { } identityKey ||
            SingleEvidenceValue(envelope, "accepted-at:") is not { } acceptedText ||
            !DateTimeOffset.TryParseExact(acceptedText, "O", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTimeOffset acceptedAt) ||
            acceptedAt.Offset != TimeSpan.Zero)
        {
            return false;
        }

        CoarseCommandIdentityRecord? identity = await ReadIdentityAsync(identityKey, cancellationToken).ConfigureAwait(false);
        if (identity?.DomainReservation is not { } reservation ||
            !string.Equals(identity.TenantId, envelope.TenantId, StringComparison.Ordinal) ||
            !string.Equals(identity.CommandId, commandId, StringComparison.Ordinal) ||
            !string.Equals(identity.DomainKeyHash, domainKey, StringComparison.Ordinal) ||
            !string.Equals(reservation.CorrelationId, envelope.CorrelationId, StringComparison.Ordinal) ||
            !string.Equals(operationId, reservation.TaskId ?? commandId, StringComparison.Ordinal))
        {
            return false;
        }

        CommandSubmissionResponse outcome = new()
        {
            CommandId = commandId,
            CorrelationId = envelope.CorrelationId,
            TaskId = reservation.TaskId,
            OperationId = operationId,
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = acceptedAt,
            ReasonCode = ChatBotMessageCode.Command_accepted,
            RetryEligible = false,
        };
        AuditReplayIntent intent = new(
            AuditReplayIntentKind.PostCommitAuditReconciliation,
            envelope.TenantId,
            envelope.ActorId,
            envelope.CommandName,
            envelope.ResourceId,
            envelope.CorrelationId,
            envelope.IdempotencyKey,
            "idempotency_outcome_unavailable",
            clock.UtcNow,
            outcome,
            domainKey,
            identityKey);
        return await ReconcileOutcomeAsync(intent, cancellationToken).ConfigureAwait(false);
    }

    private static string? SingleEvidenceValue(AuditEnvelope envelope, string prefix)
    {
        string[] matches = envelope.SourceEvidenceRefs
            .Where(reference => reference.StartsWith(prefix, StringComparison.Ordinal))
            .Select(reference => reference[prefix.Length..])
            .ToArray();
        return matches.Length == 1 && !string.IsNullOrWhiteSpace(matches[0]) ? matches[0] : null;
    }

    private async ValueTask<CoarseIdempotencyDecision> IdentityDecisionAsync(
        CoarseCommandIdentityRecord identity,
        CoarseIdempotencyRecord proposed,
        CoarseIdempotencyMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(identity.CallerFingerprint, proposed.CallerFingerprint, StringComparison.Ordinal))
        {
            return CoarseIdempotencyDecision.Conflict(metadata);
        }

        if (identity.PriorOutcome is not null)
        {
            if (identity.DomainReservation is { } reservation &&
                DomainReceiptKey(reservation.OperationClass, reservation.CoarseKeyHash) is { } receiptKey)
            {
                (CoarseIdempotencyRecord? primary, _) = await ReadDomainAsync(reservation.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
                bool primaryStillPending = primary is not null && primary.PriorOutcome is null && SameReservation(primary, reservation);
                if ((reservation.ExpiresAt > clock.UtcNow || primaryStillPending) &&
                    !await EnsureDomainReceiptAsync(receiptKey, reservation, identity.PriorOutcome, cancellationToken).ConfigureAwait(false))
                {
                    return CoarseIdempotencyDecision.RecoveryPending(metadata);
                }
            }

            return CoarseIdempotencyDecision.ReplayPriorOutcome(metadata, Clone(identity.PriorOutcome));
        }

        // A domain outcome can commit before its identity update wins the ETag race.
        // Read through the domain so a retry never dispatches the same command again.
        (CoarseIdempotencyRecord? domain, _) = await ReadDomainAsync(identity.DomainKeyHash, cancellationToken).ConfigureAwait(false);
        if (domain?.PriorOutcome is null && identity.DomainReservation is { } pendingReservation &&
            DomainReceiptKey(pendingReservation.OperationClass, pendingReservation.CoarseKeyHash) is { } pendingReceiptKey)
        {
            (CoarseIdempotencyRecord? receipt, _) = await ReadDomainAsync(pendingReceiptKey, cancellationToken).ConfigureAwait(false);
            domain = receipt?.PriorOutcome is not null ? receipt : domain;
        }
        if (domain?.PriorOutcome is null)
        {
            return CoarseIdempotencyDecision.RecoveryPending(metadata);
        }

        if (identity.DomainReservation is { } committedReservation &&
            DomainReceiptKey(committedReservation.OperationClass, committedReservation.CoarseKeyHash) is { } committedReceiptKey &&
            !await EnsureDomainReceiptAsync(committedReceiptKey, committedReservation, domain.PriorOutcome, cancellationToken).ConfigureAwait(false))
        {
            return CoarseIdempotencyDecision.RecoveryPending(metadata);
        }

        (CoarseCommandIdentityRecord? current, string etag) = await ReadIdentityWithEtagAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
        if (current is not null &&
            string.Equals(current.CallerFingerprint, proposed.CallerFingerprint, StringComparison.Ordinal) &&
            current.PriorOutcome is null)
        {
            _ = await _state.TrySaveIdentityAsync(proposed.IdentityKeyHash!,
                current with { PriorOutcome = Clone(domain.PriorOutcome) }, etag, cancellationToken).ConfigureAwait(false);
        }

        return CoarseIdempotencyDecision.ReplayPriorOutcome(metadata, Clone(domain.PriorOutcome));
    }

    private async ValueTask<CoarseIdempotencyDecision> DomainDecisionAsync(
        CoarseIdempotencyRecord existing,
        CoarseIdempotencyRecord proposed,
        CoarseIdempotencyMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(existing.CanonicalEquivalenceHash, proposed.CanonicalEquivalenceHash, StringComparison.Ordinal))
        {
            return CoarseIdempotencyDecision.Conflict(metadata);
        }

        if (existing.PriorOutcome is null && existing.IdentityKeyHash is { } recoveringIdentityKey &&
            _pendingOutcomes.TryGetValue(recoveringIdentityKey, out CommandSubmissionResponse? recoveryOutcome))
        {
            try
            {
                await RecordOutcomeAsync(Metadata(existing), recoveryOutcome, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return CoarseIdempotencyDecision.RecoveryPending(metadata);
            }
        }

        if (existing.PriorOutcome is null && existing.IdentityKeyHash is { } identityKey)
        {
            CoarseCommandIdentityRecord? identity = await ReadIdentityAsync(identityKey, cancellationToken).ConfigureAwait(false);
            CommandSubmissionResponse? durableOutcome = identity?.PriorOutcome;
            if (durableOutcome is null)
            {
                (CoarseIdempotencyRecord? primary, _) = await ReadDomainAsync(existing.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
                durableOutcome = primary?.PriorOutcome;
            }

            if (durableOutcome is not null)
            {
                string? receiptKey = DomainReceiptKey(existing.OperationClass, existing.CoarseKeyHash);
                if (receiptKey is not null &&
                    !await EnsureDomainReceiptAsync(receiptKey, existing, durableOutcome, cancellationToken).ConfigureAwait(false))
                {
                    return CoarseIdempotencyDecision.RecoveryPending(metadata);
                }

                existing = existing with { PriorOutcome = Clone(durableOutcome) };
            }
        }

        return existing.PriorOutcome is null
            ? CoarseIdempotencyDecision.RecoveryPending(metadata)
            : await ClaimReplayIdentityAsync(existing, proposed, metadata, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<CoarseIdempotencyDecision> ClaimReplayIdentityAsync(
        CoarseIdempotencyRecord existing,
        CoarseIdempotencyRecord proposed,
        CoarseIdempotencyMetadata metadata,
        CancellationToken cancellationToken)
    {
        CoarseCommandIdentityRecord identity = new(
            proposed.TenantId, proposed.CommandId, proposed.CallerFingerprint!, existing.CoarseKeyHash,
            clock.UtcNow, Clone(existing.PriorOutcome!));
        bool claimed = await _state.TrySaveIdentityAsync(proposed.IdentityKeyHash!, identity, string.Empty, cancellationToken).ConfigureAwait(false);
        if (claimed)
        {
            return CoarseIdempotencyDecision.ReplayPriorOutcome(metadata, Clone(existing.PriorOutcome!));
        }

        CoarseCommandIdentityRecord? owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
        return owner is null ? CoarseIdempotencyDecision.Conflict(metadata) :
            await IdentityDecisionAsync(owner, proposed, metadata, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask DeletePendingDomainForMetadataAsync(
        string key,
        CoarseIdempotencyMetadata metadata,
        CancellationToken cancellationToken)
    {
        (CoarseIdempotencyRecord? record, _) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        if (record is not null &&
            string.Equals(record.OperationClass, metadata.OperationClass, StringComparison.Ordinal) &&
            string.Equals(record.CoarseKeyHash, metadata.CoarseKeyHash, StringComparison.Ordinal) &&
            string.Equals(record.CanonicalEquivalenceHash, metadata.CanonicalEquivalenceHash, StringComparison.Ordinal) &&
            string.Equals(record.IdentityKeyHash, metadata.IdentityKeyHash, StringComparison.Ordinal) &&
            string.Equals(record.ReservationId, metadata.ReservationId, StringComparison.Ordinal) &&
            (metadata.CreatedAt is null || record.CreatedAt == metadata.CreatedAt))
        {
            await DeletePendingDomainAsync(key, record, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask DeletePendingDomainAsync(string key, CoarseIdempotencyRecord expected, CancellationToken cancellationToken)
    {
        (CoarseIdempotencyRecord? record, string etag) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        if (record is not null && record.PriorOutcome is null && SameReservation(record, expected) &&
            !string.IsNullOrWhiteSpace(etag))
        {
            await DeletePendingAsync(key, etag, null, expected, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask DeleteOwnedPendingIdentityAsync(
        string key,
        CoarseIdempotencyRecord expected,
        CancellationToken cancellationToken)
    {
        (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false);
        if (identity is not null && identity.PriorOutcome is null &&
            identity.CreatedAt == expected.CreatedAt &&
            string.Equals(identity.DomainKeyHash, expected.CoarseKeyHash, StringComparison.Ordinal) &&
            string.Equals(identity.CallerFingerprint, expected.CallerFingerprint, StringComparison.Ordinal) &&
            string.Equals(identity.DomainReservation?.ReservationId, expected.ReservationId, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(etag))
        {
            await DeletePendingAsync(key, etag, identity, null, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask DeletePendingAsync(
        string key,
        string etag,
        CoarseCommandIdentityRecord? originalIdentity,
        CoarseIdempotencyRecord? originalDomain,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (await _state.TryDeleteAsync(key, etag, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            // A concurrent outcome can win while cleanup is in progress. It must never be deleted.
            if (originalIdentity is not null)
            {
                (CoarseCommandIdentityRecord? identity, string identityEtag) = await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false);
                if (identity is null)
                {
                    return;
                }

                if (identity.CreatedAt != originalIdentity.CreatedAt ||
                    !string.Equals(identity.DomainKeyHash, originalIdentity.DomainKeyHash, StringComparison.Ordinal) ||
                    !string.Equals(identity.CallerFingerprint, originalIdentity.CallerFingerprint, StringComparison.Ordinal) ||
                    !string.Equals(identity.DomainReservation?.ReservationId, originalIdentity.DomainReservation?.ReservationId, StringComparison.Ordinal))
                {
                    return;
                }

                if (identity.PriorOutcome is not null)
                {
                    throw new InvalidOperationException("A committed outcome cannot be aborted.");
                }

                etag = identityEtag;
                continue;
            }

            (CoarseIdempotencyRecord? domain, string domainEtag) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
            if (domain is null)
            {
                return;
            }

            if (!SameReservation(domain, originalDomain!))
            {
                return;
            }

            if (domain.PriorOutcome is not null)
            {
                throw new InvalidOperationException("A committed outcome cannot be aborted.");
            }

            etag = domainEtag;
        }

        throw new InvalidOperationException("Pending admission cleanup did not complete.");
    }

    private async ValueTask<bool> EnsureIdentityOutcomeAsync(
        string? identityKey,
        string domainKey,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        if (identityKey is null)
        {
            return false;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
            if (identity is null || identity.DomainKeyHash != domainKey)
            {
                return false;
            }

            if (identity.PriorOutcome is not null)
            {
                return true;
            }

            if (await _state.TrySaveIdentityAsync(identityKey,
                identity with { PriorOutcome = Clone(outcome) }, etag, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask<bool> EnsureIdentityOutcomeAsync(CoarseIdempotencyRecord domain, CancellationToken cancellationToken)
    {
        if (domain.IdentityKeyHash is not { } identityKey || domain.PriorOutcome is null)
        {
            return true;
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
            if (identity is null)
            {
                CoarseCommandIdentityRecord restored = new(
                    domain.TenantId, domain.CommandId, domain.CallerFingerprint!, domain.CoarseKeyHash,
                    domain.CreatedAt, Clone(domain.PriorOutcome));
                if (await _state.TrySaveIdentityAsync(identityKey, restored, string.Empty, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }

                continue;
            }

            if (identity.DomainKeyHash != domain.CoarseKeyHash ||
                !string.Equals(identity.CallerFingerprint, domain.CallerFingerprint, StringComparison.Ordinal))
            {
                return false;
            }

            if (identity.PriorOutcome is not null ||
                await _state.TrySaveIdentityAsync(identityKey, identity with { PriorOutcome = Clone(domain.PriorOutcome) },
                    etag, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask<bool> EnsurePrimaryDomainOutcomeAsync(
        string key,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            (CoarseIdempotencyRecord? record, string etag) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
            if (record is null)
            {
                return false;
            }

            if (record.PriorOutcome is not null)
            {
                return string.Equals(record.PriorOutcome.CommandId, outcome.CommandId, StringComparison.Ordinal);
            }

            if (await _state.TrySaveDomainAsync(key, record with { PriorOutcome = Clone(outcome) }, etag, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask<bool> EnsureDomainReceiptAsync(
        string receiptKey,
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        (CoarseCommandIdentityRecord? identity, _) = metadata.IdentityKeyHash is { } identityKey
            ? await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false)
            : (null, string.Empty);
        CoarseIdempotencyRecord? reservation = identity?.DomainReservation;
        if (reservation is null)
        {
            (reservation, _) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
        }

        return reservation is not null &&
            await EnsureDomainReceiptAsync(receiptKey, reservation, outcome, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<bool> EnsureDomainReceiptAsync(
        string receiptKey,
        CoarseIdempotencyRecord reservation,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            (CoarseIdempotencyRecord? current, string etag) = await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
            if (current is not null &&
                (!string.Equals(current.IdentityKeyHash, reservation.IdentityKeyHash, StringComparison.Ordinal) ||
                 !string.Equals(current.CanonicalEquivalenceHash, reservation.CanonicalEquivalenceHash, StringComparison.Ordinal)))
            {
                return false;
            }

            if (current?.PriorOutcome is not null)
            {
                return string.Equals(current.PriorOutcome.CommandId, outcome.CommandId, StringComparison.Ordinal);
            }

            CoarseIdempotencyRecord updated = (current ?? reservation) with { PriorOutcome = Clone(outcome) };
            if (await _state.TrySaveDomainAsync(receiptKey, updated, etag, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private static string? DomainReceiptKey(string operationClass, string domainKey)
        => string.Equals(operationClass, CoarseIdempotencyOperationClass.CommandExecution.Code, StringComparison.Ordinal)
            ? null
            : "domain-receipt:" + domainKey;

    private static bool SameReservation(CoarseIdempotencyRecord current, CoarseIdempotencyRecord original)
        => string.Equals(current.TenantId, original.TenantId, StringComparison.Ordinal) &&
            string.Equals(current.OperationClass, original.OperationClass, StringComparison.Ordinal) &&
            string.Equals(current.CoarseKeyHash, original.CoarseKeyHash, StringComparison.Ordinal) &&
            string.Equals(current.CanonicalEquivalenceHash, original.CanonicalEquivalenceHash, StringComparison.Ordinal) &&
            string.Equals(current.CommandId, original.CommandId, StringComparison.Ordinal) &&
            string.Equals(current.IdentityKeyHash, original.IdentityKeyHash, StringComparison.Ordinal) &&
            string.Equals(current.ReservationId, original.ReservationId, StringComparison.Ordinal) &&
            current.CreatedAt == original.CreatedAt;

    private async ValueTask RecoverPendingOutcomeAsync(
        string identityKey,
        CoarseCommandIdentityRecord identity,
        CancellationToken cancellationToken)
    {
        if (identity.PriorOutcome is not null || identity.DomainReservation is not { } reservation)
        {
            return;
        }

        if (_pendingOutcomes.TryGetValue(identityKey, out CommandSubmissionResponse? outcome))
        {
            try
            {
                await RecordOutcomeAsync(Metadata(reservation), outcome, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A retained post-commit envelope can still restore the receipt after this attempt fails.
            }
        }

        if (_auditHistory is null)
        {
            return;
        }

        try
        {
            foreach (AuditEnvelope envelope in _auditHistory.GetPostCommitEnvelopes(identity.TenantId, identity.CommandId))
            {
                if (await ReconcileOutcomeFromAuditAsync(envelope, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The reservation remains in place and the gateway returns a safe retryable 503.
        }
    }

    private async ValueTask<(CoarseIdempotencyRecord? Record, string Etag)> ReadDomainAsync(string key, CancellationToken cancellationToken)
        => await _state.ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);

    private async ValueTask<CoarseCommandIdentityRecord?> ReadIdentityAsync(string key, CancellationToken cancellationToken)
        => (await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false)).Record;

    private async ValueTask<(CoarseCommandIdentityRecord? Record, string Etag)> ReadIdentityWithEtagAsync(string key, CancellationToken cancellationToken)
        => await _state.ReadIdentityAsync(key, cancellationToken).ConfigureAwait(false);

    private static CoarseIdempotencyMetadata Metadata(CoarseIdempotencyRecord record)
        => new(record.OperationClass, record.CoarseKeyHash, record.CanonicalEquivalenceHash, record.ExpiresAt,
            record.IdentityKeyHash, record.CreatedAt, record.ReservationId);

    private static CommandSubmissionResponse Clone(CommandSubmissionResponse outcome)
        => new()
        {
            CommandId = outcome.CommandId,
            CorrelationId = outcome.CorrelationId,
            TaskId = outcome.TaskId,
            OperationId = outcome.OperationId,
            LifecycleState = outcome.LifecycleState,
            AcceptedAt = outcome.AcceptedAt,
            ReasonCode = outcome.ReasonCode,
            RetryEligible = outcome.RetryEligible,
        };
}
