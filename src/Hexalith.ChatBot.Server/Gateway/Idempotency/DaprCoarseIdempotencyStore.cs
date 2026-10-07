using System.Collections.Concurrent;

using Dapr.Client;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class DaprCoarseIdempotencyStore : IIdempotencyStore
{
    private readonly ICoarseIdempotencyStateClient _state;
    private readonly ISystemClock clock;
    private readonly IAuditHistoryReader? _auditHistory;
    private readonly IEventStoreGatewayClient? _eventStore;
    internal static readonly TimeSpan ReservationLease = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<string, CommandSubmissionResponse> _pendingOutcomes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _unadmittedReservations = new(StringComparer.Ordinal);

    public DaprCoarseIdempotencyStore(DaprClient client, ISystemClock clock, IAuditHistoryReader auditHistory)
        : this(new DaprCoarseIdempotencyStateClient(client), clock, auditHistory)
    {
    }

    public DaprCoarseIdempotencyStore(DaprClient client, ISystemClock clock, IAuditHistoryReader auditHistory, IEventStoreGatewayClient eventStore)
        : this(new DaprCoarseIdempotencyStateClient(client), clock, auditHistory, eventStore)
    {
    }

    internal DaprCoarseIdempotencyStore(ICoarseIdempotencyStateClient state, ISystemClock clock, IAuditHistoryReader? auditHistory = null, IEventStoreGatewayClient? eventStore = null)
    {
        _state = state;
        this.clock = clock;
        _auditHistory = auditHistory;
        _eventStore = eventStore;
    }

    /// <summary>Gets the conditional state seam that persists identity, domain, and receipt records.</summary>
    internal ICoarseIdempotencyStateClient StateClient => _state;

    /// <summary>Gets the retained audit history used to reconcile a lost receipt, when wired.</summary>
    internal IAuditHistoryReader? AuditHistory => _auditHistory;

    /// <summary>Gets the authoritative EventStore command-status source used for durable recovery, when wired.</summary>
    internal IEventStoreGatewayClient? EventStore => _eventStore;

    public async ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(
        ChatBotGatewayContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        DateTimeOffset now = clock.UtcNow;
        CoarseIdempotencyRecord proposed = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, now)
            with
            {
                ReservationId = Guid.NewGuid().ToString("N"),
                DispatchState = CoarseDispatchState.Reserved,
                ReservationLeaseExpiresAt = now.Add(ReservationLease),
            };
        CoarseIdempotencyMetadata metadata = Metadata(proposed);
        context.SetIdempotency(metadata);

        CoarseCommandIdentityRecord? owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
        if (owner is not null && CanRelease(owner.DomainReservation) && owner.PriorOutcome is null)
        {
            bool released = await TryReleaseUnadmittedIdentityAsync(proposed.IdentityKeyHash!, owner, cancellationToken).ConfigureAwait(false);
            owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
            if (!released && owner is not null && CanRelease(owner.DomainReservation) && owner.PriorOutcome is null)
            {
                return CoarseIdempotencyDecision.RecoveryPending(metadata);
            }
        }

        if (owner is not null)
        {
            if (string.Equals(owner.CallerFingerprint, proposed.CallerFingerprint, StringComparison.Ordinal))
            {
                await RecoverPendingOutcomeAsync(proposed.IdentityKeyHash!, owner, cancellationToken).ConfigureAwait(false);
                owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false) ?? owner;
            }

            return await IdentityDecisionAsync(owner, proposed, metadata, cancellationToken).ConfigureAwait(false);
        }

        // Old generic records used a body-derived key and had no caller-ID index. Keep their unchanged-body replay
        // during the upgrade window; expiry never proves that an unresolved historical dispatch did not commit.
        if (proposed.LegacyKeyHash is { } legacyKey)
        {
            (CoarseIdempotencyRecord? legacy, _) = await ReadDomainAsync(legacyKey, cancellationToken).ConfigureAwait(false);
            if (legacy is not null && (legacy.ExpiresAt > now ||
                legacy.PriorOutcome is null && legacy.DispatchState == CoarseDispatchState.Unknown))
            {
                return legacy.PriorOutcome is null
                    ? CoarseIdempotencyDecision.RecoveryPending(metadata)
                    : await ClaimReplayIdentityAsync(legacy, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }
        }

        string? receiptKey = DomainReceiptKey(proposed.OperationClass, proposed.CoarseKeyHash);
        if (!await TryReleaseUnadmittedDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false) ||
            !await TryReleaseUnadmittedDomainAsync(proposed.CoarseKeyHash, cancellationToken).ConfigureAwait(false))
        {
            return CoarseIdempotencyDecision.RecoveryPending(metadata);
        }

        if (receiptKey is not null)
        {
            (CoarseIdempotencyRecord? receipt, string receiptEtag) = await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
            if (receipt is not null)
            {
                if (receipt.PriorOutcome is not null && receipt.ExpiresAt <= now)
                {
                    // Preserve the caller receipt before logically expiring the sole domain copy.
                    if (!await EnsureIdentityOutcomeAsync(receipt, cancellationToken).ConfigureAwait(false))
                    {
                        return CoarseIdempotencyDecision.RecoveryPending(metadata);
                    }
                    (CoarseIdempotencyRecord? primary, _) = await ReadDomainAsync(proposed.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
                    if (primary is not null && primary.PriorOutcome is null && SameReservation(primary, receipt))
                    {
                        return await DomainDecisionAsync(receipt, proposed, metadata, cancellationToken).ConfigureAwait(false);
                    }

                    if (!await _state.TrySaveDomainAsync(receiptKey, receipt with { Released = true }, receiptEtag, cancellationToken).ConfigureAwait(false))
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
                return await DomainDecisionAsync(existing, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }

            if (!await EnsureIdentityOutcomeAsync(existing, cancellationToken).ConfigureAwait(false))
            {
                return CoarseIdempotencyDecision.RecoveryPending(metadata);
            }

            if (!await _state.TrySaveDomainAsync(proposed.CoarseKeyHash, existing with { Released = true }, etag, cancellationToken).ConfigureAwait(false))
            {
                return CoarseIdempotencyDecision.RecoveryPending(metadata);
            }
            (existing, etag) = await ReadDomainAsync(proposed.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return await DomainDecisionAsync(existing, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }
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
                return existing is null ? CoarseIdempotencyDecision.RecoveryPending(metadata) :
                    await DomainDecisionAsync(existing, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }

            (CoarseIdempotencyRecord? receiptOwner, string receiptVersion) = receiptKey is null
                ? (null, string.Empty)
                : await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
            if (receiptKey is not null &&
                (receiptOwner is not null ||
                 !await _state.TrySaveDomainAsync(receiptKey, proposed, receiptVersion, cancellationToken).ConfigureAwait(false)))
            {
                await ReleasePendingDomainAsync(proposed.CoarseKeyHash, proposed, cancellationToken).ConfigureAwait(false);
                (CoarseIdempotencyRecord? currentReceipt, _) = await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
                return currentReceipt is null ? CoarseIdempotencyDecision.RecoveryPending(metadata) :
                    await DomainDecisionAsync(currentReceipt, proposed, metadata, cancellationToken).ConfigureAwait(false);
            }

            CoarseCommandIdentityRecord identity = new(
                proposed.TenantId, proposed.CommandId, proposed.CallerFingerprint!, proposed.CoarseKeyHash, now, null, proposed);
            (CoarseCommandIdentityRecord? claimOwner, string claimVersion) = await ReadIdentityWithEtagAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
            bool claimed = claimOwner is null && await _state.TrySaveIdentityAsync(proposed.IdentityKeyHash!, identity, claimVersion, cancellationToken).ConfigureAwait(false);
            if (claimed)
            {
                return CoarseIdempotencyDecision.Proceed(metadata);
            }

            owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
            if (receiptKey is not null)
            {
                await ReleasePendingDomainAsync(receiptKey, proposed, cancellationToken).ConfigureAwait(false);
            }
            await ReleasePendingDomainAsync(proposed.CoarseKeyHash, proposed, cancellationToken).ConfigureAwait(false);
            return owner is null ? CoarseIdempotencyDecision.RecoveryPending(metadata) :
                await IdentityDecisionAsync(owner, proposed, metadata, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            RememberUnadmitted(proposed.ReservationId);
            try
            {
                await AbortAdmissionCoreAsync(Metadata(proposed), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception cleanupFailure) when (cleanupFailure is not OperationCanceledException)
            {
                // The committed reservation stays marked so the next admission can remove it
                // after the state store accepts conditional releases again.
            }

            try
            {
                if (!await UnadmittedReservationRemainsAsync(Metadata(proposed), cancellationToken).ConfigureAwait(false))
                {
                    ForgetUnadmitted(proposed.ReservationId);
                }
            }
            catch (Exception readFailure) when (readFailure is not OperationCanceledException)
            {
                // Keep the reservation marked when the store cannot confirm cleanup.
            }

            throw;
        }
    }

    /// <summary>Atomically fences dispatch and retains the exact response before the external call.</summary>
    public async ValueTask<bool> PrepareDispatchAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        if (metadata.IdentityKeyHash is not { } key)
        {
            return false;
        }

        (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false);
        CoarseIdempotencyRecord? reservation = identity?.DomainReservation;
        if (identity?.PriorOutcome is not null || reservation is null ||
            !MatchesMetadata(reservation, metadata) || reservation.DispatchState != CoarseDispatchState.Reserved ||
            reservation.ReservationLeaseExpiresAt is not { } lease || lease <= clock.UtcNow ||
            !string.Equals(outcome.CommandId, reservation.CommandId, StringComparison.Ordinal) ||
            !string.Equals(outcome.CorrelationId, reservation.CorrelationId, StringComparison.Ordinal) ||
            !string.Equals(outcome.OperationId, reservation.TaskId ?? reservation.CommandId, StringComparison.Ordinal) ||
            outcome.AcceptedAt.Offset != TimeSpan.Zero)
        {
            return false;
        }

        (CoarseIdempotencyRecord? domain, _) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
        if (domain is null || !SameReservation(domain, reservation) || domain.DispatchState != CoarseDispatchState.Reserved)
        {
            return false;
        }

        CoarseIdempotencyRecord prepared = reservation with
        {
            DispatchState = CoarseDispatchState.Dispatching,
            PreparedOutcome = Clone(outcome),
        };
        try
        {
            return await _state.TrySaveIdentityAsync(key, identity! with { DomainReservation = prepared }, etag, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // This method has not returned to the gateway, so external dispatch has not
            // started. An acknowledged-loss write can be reclaimed only after this exact
            // prepared ownership is durably fenced off; ordinary abort still denies Dispatching.
            try
            {
                (CoarseCommandIdentityRecord? current, string currentEtag) = await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false);
                if (current?.PriorOutcome is null && current?.DomainReservation is { } currentReservation &&
                    currentReservation.DispatchState == CoarseDispatchState.Dispatching &&
                    SameReservation(currentReservation, prepared) &&
                    currentReservation.PreparedOutcome is { } currentOutcome &&
                    SamePreparedOutcome(currentOutcome, outcome) &&
                    await _state.TrySaveIdentityAsync(key,
                        current with { DomainReservation = currentReservation with { DispatchState = CoarseDispatchState.Aborted } },
                        currentEtag, cancellationToken).ConfigureAwait(false))
                {
                    await AbortAdmissionCoreAsync(metadata, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception cleanupFailure) when (cleanupFailure is not OperationCanceledException)
            {
                // Retain the conservative dispatch fence if abort ownership cannot be persisted.
            }

            throw;
        }
    }

    /// <summary>Uses the prepared owner's conditional identity write to retain the actual dispatch-plan target.</summary>
    public async ValueTask<bool> BindDispatchTargetAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse preparedOutcome,
        string aggregateId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId) || metadata.IdentityKeyHash is not { } key)
        {
            return false;
        }

        (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false);
        if (identity?.PriorOutcome is not null || identity?.DomainReservation is not { } reservation ||
            !MatchesMetadata(reservation, metadata) || reservation.DispatchState != CoarseDispatchState.Dispatching ||
            reservation.PreparedOutcome is not { } prepared || !SamePreparedOutcome(prepared, preparedOutcome) ||
            reservation.PreparedAggregateId is { } target && !string.Equals(target, aggregateId, StringComparison.Ordinal))
        {
            return false;
        }

        (CoarseIdempotencyRecord? domain, _) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
        if (domain is null || !SameReservation(domain, reservation))
        {
            return false;
        }

        // Even an already matching target must win a current ownership CAS. An older read cannot authorize submission
        // after a concurrent release/replacement. An acknowledgement loss is handled by the gateway's existing
        // known-undispatched release, unless planning may already have attempted another external write.
        return await _state.TrySaveIdentityAsync(key,
            identity! with { DomainReservation = reservation with { PreparedAggregateId = aggregateId } },
            etag, cancellationToken).ConfigureAwait(false);
    }

    private static bool SamePreparedOutcome(CommandSubmissionResponse actual, CommandSubmissionResponse expected)
        => actual.CommandId == expected.CommandId && actual.CorrelationId == expected.CorrelationId &&
            actual.TaskId == expected.TaskId && actual.OperationId == expected.OperationId &&
            actual.LifecycleState == expected.LifecycleState && actual.AcceptedAt == expected.AcceptedAt &&
            actual.ReasonCode == expected.ReasonCode && actual.RetryEligible == expected.RetryEligible;

    /// <summary>CAS-fences observed SDK acceptance onto the current exact prepared dispatch reservation.</summary>
    public async ValueTask<bool> ConfirmSdkSubmissionAcceptedAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse preparedOutcome,
        string aggregateId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aggregateId) || metadata.IdentityKeyHash is not { } key)
        {
            return false;
        }

        (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false);
        if (identity?.PriorOutcome is not null || identity?.DomainReservation is not { } reservation ||
            !MatchesMetadata(reservation, metadata) || reservation.OperationClass != metadata.OperationClass ||
            reservation.DispatchState != CoarseDispatchState.Dispatching ||
            reservation.PreparedOutcome is not { } prepared || !SamePreparedOutcome(prepared, preparedOutcome) ||
            !string.Equals(reservation.PreparedAggregateId, aggregateId, StringComparison.Ordinal))
        {
            return false;
        }

        (CoarseIdempotencyRecord? domain, _) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
        if (domain is null || !SameReservation(domain, reservation))
        {
            return false;
        }

        // Winning a current ETag is required even if confirmation was already present. A paused older owner cannot
        // authorize receipt persistence after another owner replaces it. A committed write remains proof even when
        // its acknowledgement is lost; the gateway still returns its existing safe receipt-failure response.
        return await _state.TrySaveIdentityAsync(key,
            identity! with { DomainReservation = reservation with { SdkSubmissionAccepted = true } },
            etag, cancellationToken).ConfigureAwait(false);
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
            identityStored = await EnsureIdentityOutcomeAsync(metadata.IdentityKeyHash, metadata, stored, cancellationToken).ConfigureAwait(false);
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
            domainStored = await EnsurePrimaryDomainOutcomeAsync(metadata, stored, cancellationToken).ConfigureAwait(false);
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
        RememberUnadmitted(metadata.ReservationId);
        try
        {
            await AbortAdmissionCoreAsync(metadata, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (!await UnadmittedReservationRemainsAsync(metadata, cancellationToken).ConfigureAwait(false))
                {
                    ForgetUnadmitted(metadata.ReservationId);
                }
            }
            catch (Exception readFailure) when (readFailure is not OperationCanceledException)
            {
                // Keep the reservation marked when the store cannot confirm cleanup.
            }
        }
    }

    /// <summary>
    /// Releases prepared ownership only with the dispatcher's proof that nothing committed (no external write was
    /// attempted, or EventStore definitively refused the only one).
    /// </summary>
    public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse preparedOutcome,
        CancellationToken cancellationToken)
        => AbortAdmissionCoreAsync(metadata, cancellationToken, preparedOutcome);

    private async ValueTask AbortAdmissionCoreAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken,
        CommandSubmissionResponse? undispatchedOutcome = null)
    {
        if (!await FenceAbortAsync(metadata, cancellationToken, undispatchedOutcome).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Admission ownership cannot be aborted after dispatch or replacement.");
        }

        if (metadata.IdentityKeyHash is { } identityKey)
        {
            (CoarseCommandIdentityRecord? identity, string identityEtag) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
            if (identity is not null && identity.DomainKeyHash == metadata.CoarseKeyHash &&
                identity.PriorOutcome is null &&
                (metadata.CreatedAt is null || identity.CreatedAt == metadata.CreatedAt) &&
                string.Equals(identity.DomainReservation?.ReservationId, metadata.ReservationId, StringComparison.Ordinal))
            {
                await ReleasePendingAsync(identityKey, identityEtag, identity, null, cancellationToken).ConfigureAwait(false);
            }
        }

        if (DomainReceiptKey(metadata.OperationClass, metadata.CoarseKeyHash) is { } receiptKey)
        {
            await ReleasePendingDomainForMetadataAsync(receiptKey, metadata, cancellationToken).ConfigureAwait(false);
        }

        await ReleasePendingDomainForMetadataAsync(metadata.CoarseKeyHash, metadata, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<bool> FenceAbortAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken,
        CommandSubmissionResponse? undispatchedOutcome = null)
    {
        // The identity is the dispatch authority. CAS it before releasing any domain copies.
        if (metadata.IdentityKeyHash is { } identityKey)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                (CoarseCommandIdentityRecord? identity, string etag) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
                if (identity is null)
                {
                    break;
                }

                if (identity.DomainReservation is not { } reservation || !MatchesMetadata(reservation, metadata))
                {
                    // A caller-ID winner in another domain does not own this losing reservation.
                    // Leave that identity untouched and fence the matching domain below.
                    break;
                }

                if (identity.PriorOutcome is not null || reservation.SdkSubmissionAccepted || reservation.DispatchState == CoarseDispatchState.Unknown ||
                    reservation.DispatchState == CoarseDispatchState.Dispatching &&
                    (undispatchedOutcome is null || reservation.PreparedOutcome is not { } prepared ||
                     !SamePreparedOutcome(prepared, undispatchedOutcome)))
                {
                    return false;
                }

                if (reservation.DispatchState == CoarseDispatchState.Aborted ||
                    await _state.TrySaveIdentityAsync(identityKey,
                        identity with { DomainReservation = reservation with { DispatchState = CoarseDispatchState.Aborted } },
                        etag, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
                if (attempt == 2)
                {
                    return false;
                }
            }
        }

        // A failed first-write can leave a domain reservation without an identity. Its
        // bounded lease is also checked at dispatch, so an old owner cannot dispatch it.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            (CoarseIdempotencyRecord? domain, string etag) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
            if (domain is null || !MatchesMetadata(domain, metadata))
            {
                return true;
            }

            if (domain.PriorOutcome is not null || domain.DispatchState is CoarseDispatchState.Unknown or CoarseDispatchState.Dispatching)
            {
                return false;
            }

            if (domain.DispatchState == CoarseDispatchState.Aborted ||
                await _state.TrySaveDomainAsync(metadata.CoarseKeyHash,
                    domain with { DispatchState = CoarseDispatchState.Aborted }, etag, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesMetadata(CoarseIdempotencyRecord reservation, CoarseIdempotencyMetadata metadata)
        => string.Equals(reservation.ReservationId, metadata.ReservationId, StringComparison.Ordinal) &&
            string.Equals(reservation.IdentityKeyHash, metadata.IdentityKeyHash, StringComparison.Ordinal) &&
            string.Equals(reservation.CoarseKeyHash, metadata.CoarseKeyHash, StringComparison.Ordinal) &&
            string.Equals(reservation.CanonicalEquivalenceHash, metadata.CanonicalEquivalenceHash, StringComparison.Ordinal) &&
            reservation.CreatedAt == metadata.CreatedAt;

    /// <summary>Reconciles a post-dispatch, metadata-only receipt from the independent audit replay queue.</summary>
    internal async ValueTask<bool> ReconcileOutcomeAsync(
        AuditReplayIntent intent,
        CancellationToken cancellationToken,
        CoarseIdempotencyMetadata? retry = null)
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
            !string.Equals(reservation.RequesterId, intent.ActorId, StringComparison.Ordinal) ||
            !string.Equals(identity.CommandId, outcome.CommandId, StringComparison.Ordinal) ||
            !string.Equals(identity.DomainKeyHash, domainKey, StringComparison.Ordinal) ||
            (retry is not null &&
             (!string.Equals(reservation.CoarseKeyHash, retry.CoarseKeyHash, StringComparison.Ordinal) ||
              !string.Equals(reservation.CanonicalEquivalenceHash, retry.CanonicalEquivalenceHash, StringComparison.Ordinal))) ||
            !string.Equals(reservation.IdentityKeyHash, identityKey, StringComparison.Ordinal) ||
            !string.Equals(reservation.CorrelationId, outcome.CorrelationId, StringComparison.Ordinal) ||
            !string.Equals(outcome.OperationId, outcome.TaskId ?? outcome.CommandId, StringComparison.Ordinal))
        {
            return false;
        }

        if (reservation.PreparedOutcome is { } prepared &&
            (prepared.CommandId != outcome.CommandId || prepared.TaskId != outcome.TaskId ||
             prepared.OperationId != outcome.OperationId || prepared.CorrelationId != outcome.CorrelationId ||
             prepared.AcceptedAt != outcome.AcceptedAt || prepared.LifecycleState != outcome.LifecycleState ||
             prepared.ReasonCode != outcome.ReasonCode || prepared.RetryEligible != outcome.RetryEligible))
        {
            return false;
        }

        if (reservation.PreparedOutcome is { } preparedReceipt && _eventStore is not null)
        {
            CommandStatusQueryResponse? evidence = await _eventStore.GetCommandStatusAsync(identity.CommandId, cancellationToken).ConfigureAwait(false);
            if (!HasCommittedOutcomeEvidence(evidence, identity, preparedReceipt))
            {
                return false;
            }
        }

        await RecordOutcomeAsync(Metadata(reservation), outcome, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Restores a receipt from a post-commit audit envelope that the registered <see cref="IAuditHistoryReader"/> still
    /// retains, for example after this store instance is replaced while the audit history survives.
    /// </summary>
    /// <remarks>
    /// The only registered audit-history reader is the in-memory audit writer, so this path does not survive a process
    /// restart; restart recovery uses the authoritative EventStore command-status evidence instead. Cross-restart audit
    /// reconciliation requires a deployment-provided durable audit-history reader.
    /// </remarks>
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
        if (domain?.PriorOutcome is null ||
            (identity.DomainReservation is { } owned && !SameReservation(domain, owned)))
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
            if (identity is not null)
            {
                await RecoverPendingOutcomeAsync(identityKey, identity, cancellationToken).ConfigureAwait(false);
                identity = await ReadIdentityAsync(identityKey, cancellationToken).ConfigureAwait(false) ?? identity;
            }
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
        (CoarseCommandIdentityRecord? claimOwner, string claimVersion) = await ReadIdentityWithEtagAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
        bool claimed = claimOwner is null && await _state.TrySaveIdentityAsync(proposed.IdentityKeyHash!, identity, claimVersion, cancellationToken).ConfigureAwait(false);
        if (claimed)
        {
            return CoarseIdempotencyDecision.ReplayPriorOutcome(metadata, Clone(existing.PriorOutcome!));
        }

        CoarseCommandIdentityRecord? owner = await ReadIdentityAsync(proposed.IdentityKeyHash!, cancellationToken).ConfigureAwait(false);
        return owner is null ? CoarseIdempotencyDecision.RecoveryPending(metadata) :
            await IdentityDecisionAsync(owner, proposed, metadata, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ReleasePendingDomainForMetadataAsync(
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
            await ReleasePendingDomainAsync(key, record, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ReleasePendingDomainAsync(string key, CoarseIdempotencyRecord expected, CancellationToken cancellationToken)
    {
        (CoarseIdempotencyRecord? record, string etag) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        if (record is not null && record.PriorOutcome is null && SameReservation(record, expected) &&
            !string.IsNullOrWhiteSpace(etag))
        {
            await ReleasePendingAsync(key, etag, null, expected, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ReleaseOwnedPendingIdentityAsync(
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
            await ReleasePendingAsync(key, etag, identity, null, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ReleasePendingAsync(
        string key,
        string etag,
        CoarseCommandIdentityRecord? originalIdentity,
        CoarseIdempotencyRecord? originalDomain,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            // Keep the key's ETag generation alive. Physical deletion would allow a backend
            // to reuse the old ETag and authorize a paused owner's save or cleanup.
            bool released = originalIdentity is not null
                ? await _state.TrySaveIdentityAsync(key, originalIdentity with { Released = true }, etag, cancellationToken).ConfigureAwait(false)
                : await _state.TrySaveDomainAsync(key, originalDomain! with { Released = true }, etag, cancellationToken).ConfigureAwait(false);
            if (released)
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
        CoarseIdempotencyMetadata metadata,
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
            if (identity is null || identity.DomainKeyHash != metadata.CoarseKeyHash ||
                identity.DomainReservation is { } owned && !MatchesMetadata(owned, metadata))
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
                if (await _state.TrySaveIdentityAsync(identityKey, restored, etag, cancellationToken).ConfigureAwait(false))
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
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            (CoarseIdempotencyRecord? record, string etag) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
            if (record is null || !MatchesMetadata(record, metadata))
            {
                return false;
            }

            if (record.PriorOutcome is not null)
            {
                return string.Equals(record.PriorOutcome.CommandId, outcome.CommandId, StringComparison.Ordinal);
            }

            if (await _state.TrySaveDomainAsync(metadata.CoarseKeyHash, record with { PriorOutcome = Clone(outcome) }, etag, cancellationToken).ConfigureAwait(false))
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

        return reservation is not null && MatchesMetadata(reservation, metadata) &&
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
            if (current is not null && !SameReservation(current, reservation))
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

    private static bool HasCommittedOutcomeEvidence(
        CommandStatusQueryResponse? evidence,
        CoarseCommandIdentityRecord identity,
        CommandSubmissionResponse prepared)
        => evidence is not null &&
            identity.DomainReservation is { DispatchState: CoarseDispatchState.Dispatching, SdkSubmissionAccepted: true, PreparedAggregateId: { } aggregateId } &&
            !string.IsNullOrWhiteSpace(aggregateId) && !string.IsNullOrWhiteSpace(evidence.AggregateId) &&
            string.Equals(evidence.AggregateId, aggregateId, StringComparison.Ordinal) &&
            string.Equals(evidence.MessageId, identity.CommandId, StringComparison.Ordinal) &&
            string.Equals(evidence.TenantId, identity.TenantId, StringComparison.Ordinal) &&
            string.Equals(evidence.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal) &&
            string.Equals(evidence.CorrelationId, prepared.CorrelationId, StringComparison.Ordinal) &&
            Enum.IsDefined(typeof(CommandStatus), evidence.StatusCode) &&
            string.Equals(evidence.Status, ((CommandStatus)evidence.StatusCode).ToString(), StringComparison.Ordinal) &&
            (evidence.StatusCode is (int)CommandStatus.EventsStored or (int)CommandStatus.EventsPublished or
                (int)CommandStatus.Completed or (int)CommandStatus.PublishFailed) &&
            (evidence.CommittedEventSequence > 0 ||
             evidence.StatusCode == (int)CommandStatus.Completed && evidence.EventCount == 0) &&
            string.IsNullOrWhiteSpace(evidence.RejectionEventType);

    private async ValueTask RecoverPendingOutcomeAsync(
        string identityKey,
        CoarseCommandIdentityRecord identity,
        CancellationToken cancellationToken)
    {
        if (identity.PriorOutcome is not null || identity.DomainReservation is not { } reservation)
        {
            return;
        }

        if (reservation.DispatchState == CoarseDispatchState.Dispatching &&
            reservation.PreparedOutcome is { } prepared && _eventStore is not null)
        {
            try
            {
                CommandStatusQueryResponse? evidence = await _eventStore.GetCommandStatusAsync(identity.CommandId, cancellationToken).ConfigureAwait(false);
                if (HasCommittedOutcomeEvidence(evidence, identity, prepared))
                {
                    await RecordOutcomeAsync(Metadata(reservation), prepared, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // An unavailable or unproven SDK status cannot authorize acceptance or redispatch.
            }

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

    private async ValueTask<bool> TryReleaseUnadmittedIdentityAsync(
        string identityKey,
        CoarseCommandIdentityRecord owner,
        CancellationToken cancellationToken)
    {
        string? reservationId = owner.DomainReservation?.ReservationId;
        if (!CanRelease(owner.DomainReservation) || owner.PriorOutcome is not null || owner.DomainReservation is not { } reservation)
        {
            return false;
        }

        (CoarseCommandIdentityRecord? current, _) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
        if (current is not null &&
            !string.Equals(current.DomainReservation?.ReservationId, reservationId, StringComparison.Ordinal))
        {
            return false;
        }

        if (current?.PriorOutcome is not null)
        {
            ForgetUnadmitted(reservationId);
            return false;
        }

        try
        {
            if (!await FenceAbortAsync(Metadata(reservation), cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            if (DomainReceiptKey(reservation.OperationClass, reservation.CoarseKeyHash) is { } receiptKey)
            {
                await ReleasePendingDomainAsync(receiptKey, reservation, cancellationToken).ConfigureAwait(false);
            }

            await ReleasePendingDomainAsync(reservation.CoarseKeyHash, reservation, cancellationToken).ConfigureAwait(false);
            if (current is not null)
            {
                await ReleaseOwnedPendingIdentityAsync(identityKey, reservation, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }

        (CoarseCommandIdentityRecord? after, _) = await ReadIdentityWithEtagAsync(identityKey, cancellationToken).ConfigureAwait(false);
        bool released = after is null || !string.Equals(after.DomainReservation?.ReservationId, reservationId, StringComparison.Ordinal);
        if (released && after is null)
        {
            ForgetUnadmitted(reservationId);
        }

        return released && after is null;
    }

    private async ValueTask<bool> TryReleaseUnadmittedDomainAsync(string? key, CancellationToken cancellationToken)
    {
        if (key is null)
        {
            return true;
        }

        (CoarseIdempotencyRecord? record, _) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        if (record is null || record.PriorOutcome is not null || !CanRelease(record))
        {
            return true;
        }

        string reservationId = record.ReservationId!;
        try
        {
            if (record.IdentityKeyHash is { } identityKey)
            {
                CoarseCommandIdentityRecord? identity = await ReadIdentityAsync(identityKey, cancellationToken).ConfigureAwait(false);
                if (identity?.DomainReservation is { } owner && SameReservation(owner, record))
                {
                    if (!CanRelease(owner))
                    {
                        return true;
                    }
                    return await TryReleaseUnadmittedIdentityAsync(identityKey, identity, cancellationToken).ConfigureAwait(false);
                }
            }
            if (!await FenceAbortAsync(Metadata(record), cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
            await ReleasePendingDomainAsync(key, record, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }

        (CoarseIdempotencyRecord? after, _) = await ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        if (after is not null && string.Equals(after.ReservationId, reservationId, StringComparison.Ordinal) && after.PriorOutcome is null)
        {
            return false;
        }

        return true;
    }

    private async ValueTask<bool> UnadmittedReservationRemainsAsync(
        CoarseIdempotencyMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (!IsUnadmitted(metadata.ReservationId))
        {
            return false;
        }

        if (metadata.IdentityKeyHash is { } identityKey)
        {
            CoarseCommandIdentityRecord? identity = await ReadIdentityAsync(identityKey, cancellationToken).ConfigureAwait(false);
            if (identity is not null && identity.PriorOutcome is null &&
                string.Equals(identity.DomainReservation?.ReservationId, metadata.ReservationId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        (CoarseIdempotencyRecord? domain, _) = await ReadDomainAsync(metadata.CoarseKeyHash, cancellationToken).ConfigureAwait(false);
        if (domain is not null && domain.PriorOutcome is null &&
            string.Equals(domain.ReservationId, metadata.ReservationId, StringComparison.Ordinal))
        {
            return true;
        }

        if (DomainReceiptKey(metadata.OperationClass, metadata.CoarseKeyHash) is { } receiptKey)
        {
            (CoarseIdempotencyRecord? receipt, _) = await ReadDomainAsync(receiptKey, cancellationToken).ConfigureAwait(false);
            if (receipt is not null && receipt.PriorOutcome is null &&
                string.Equals(receipt.ReservationId, metadata.ReservationId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void RememberUnadmitted(string? reservationId)
    {
        if (!string.IsNullOrEmpty(reservationId))
        {
            _unadmittedReservations[reservationId] = 0;
        }
    }

    private void ForgetUnadmitted(string? reservationId)
    {
        if (!string.IsNullOrEmpty(reservationId))
        {
            _unadmittedReservations.TryRemove(reservationId, out _);
        }
    }

    private bool CanRelease(CoarseIdempotencyRecord? reservation)
        => reservation is not null &&
            (reservation.DispatchState == CoarseDispatchState.Aborted ||
             (reservation.DispatchState == CoarseDispatchState.Reserved &&
              (IsUnadmitted(reservation.ReservationId) ||
               reservation.ReservationLeaseExpiresAt is { } lease && lease <= clock.UtcNow)));

    private bool IsUnadmitted(string? reservationId)
        => !string.IsNullOrEmpty(reservationId) && _unadmittedReservations.ContainsKey(reservationId);

    private async ValueTask<(CoarseIdempotencyRecord? Record, string Etag)> ReadDomainAsync(string key, CancellationToken cancellationToken)
    {
        (CoarseIdempotencyRecord? record, string etag) = await _state.ReadDomainAsync(key, cancellationToken).ConfigureAwait(false);
        return (record?.Released is true ? null : record, etag);
    }

    private async ValueTask<CoarseCommandIdentityRecord?> ReadIdentityAsync(string key, CancellationToken cancellationToken)
        => (await ReadIdentityWithEtagAsync(key, cancellationToken).ConfigureAwait(false)).Record;

    private async ValueTask<(CoarseCommandIdentityRecord? Record, string Etag)> ReadIdentityWithEtagAsync(string key, CancellationToken cancellationToken)
    {
        (CoarseCommandIdentityRecord? record, string etag) = await _state.ReadIdentityAsync(key, cancellationToken).ConfigureAwait(false);
        return (record?.Released is true ? null : record, etag);
    }

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
