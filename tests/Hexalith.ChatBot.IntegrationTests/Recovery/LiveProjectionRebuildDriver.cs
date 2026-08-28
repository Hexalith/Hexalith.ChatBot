using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Tier-3 projection-rebuild implementation that reconstructs a fresh isolated partition exclusively from immutable
/// source-email views and the selected tenant's WORM chain.
/// </summary>
internal sealed class LiveProjectionRebuildDriver(
    IReadOnlyList<ProjectConversationSourceEmailView> immutableSourceRecords,
    IWormAuditStore wormAuditStore,
    ProjectionRebuildBaselineEvidence baselineEvidence,
    IReadModelStore readModelStore,
    IReadModelConditionalEraser readModelEraser,
    RecoveryValidationDatasetDescriptor dataset,
    LiveRecoveryValidationOptions options,
    ISystemClock clock) : IProjectionRebuildDriver
{
    /// <inheritdoc />
    public async ValueTask<ProjectionRebuildMeasurement> RebuildAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        string? validationError = options.Validate();
        if (validationError is not null)
        {
            throw new InvalidOperationException(validationError);
        }

        if (!options.Enabled || !string.Equals(testTenantRef, options.TestTenantRef, StringComparison.Ordinal) ||
            !string.Equals(datasetRef, options.DatasetRef, StringComparison.Ordinal) ||
            !ReplayTenantPolicy.IsTestTenant(testTenantRef))
        {
            throw new InvalidOperationException(
                "Projection rebuild is restricted to the enabled dataset and replay-test tenant configuration.");
        }

        using CancellationTokenSource scenarioDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scenarioDeadline.CancelAfter(options.PerScenarioTimeout);
        CancellationToken scenarioToken = scenarioDeadline.Token;
        scenarioToken.ThrowIfCancellationRequested();
        string? datasetError = dataset.Validate(
            options.DatasetRef,
            options.DatasetVersion,
            options.DatasetVolume,
            options.ProjectionSchemaVersion,
            options.ValidationPartitionRef);
        if (datasetError is not null)
        {
            throw new InvalidOperationException(datasetError);
        }

        ProjectConversationSourceEmailView[] sources = immutableSourceRecords
            .Where(source => string.Equals(source.TenantId, testTenantRef, StringComparison.Ordinal))
            .OrderBy(static source => source.IntakeId, StringComparer.Ordinal)
            .ToArray();
        if (sources.Length != dataset.SourceRecordCount || immutableSourceRecords.Count != sources.Length)
        {
            throw new InvalidOperationException("The immutable rebuild source population or tenant boundary does not match the dataset.");
        }

        if (sources.Select(static source => source.IntakeId).Distinct(StringComparer.Ordinal).Count() != sources.Length ||
            sources.Any(static source => !AuditMetadata.IsSafeStableIdentifier(source.IntakeId)))
        {
            throw new InvalidOperationException("The immutable rebuild source contains an unsafe or duplicate logical resource id.");
        }

        IReadOnlyList<WormAuditChainRecord> auditRecords = wormAuditStore.EnumerateChain(testTenantRef);
        if (auditRecords.Count != dataset.WormAuditRecordCount)
        {
            throw new InvalidOperationException("The selected tenant WORM population does not match the dataset.");
        }

        IReadOnlyList<WormOperationGroup> rebuildOperations = GroupOperations(auditRecords);
        if (baselineEvidence.WormRecordCount != dataset.WormAuditRecordCount ||
            baselineEvidence.SourceIntakeIds.Count != dataset.SourceRecordCount ||
            baselineEvidence.WormOperationCount <= 0 ||
            rebuildOperations.Count != baselineEvidence.WormOperationCount)
        {
            throw new InvalidOperationException("The independent seed and rebuild WORM cardinalities do not match the pinned dataset.");
        }

        string baselinePartitionTenant = BaselinePartitionTenant(testTenantRef, dataset.ValidationPartitionRef);
        IReadOnlyList<ProjectionResourceDigest> baseline = await ReadBaselineSnapshotAsync(
            readModelStore,
            baselinePartitionTenant,
            baselineEvidence,
            scenarioToken).ConfigureAwait(false);
        string preRebuildSchemaVersion = baselineEvidence.ProjectionSchemaVersion;
        string freshPartitionTenant = FreshPartitionTenant(testTenantRef, dataset.ValidationPartitionRef, correlationId);
        IReadOnlyList<string> freshKeys = ProjectionKeys(freshPartitionTenant, sources, auditRecords);
        int expectedFreshKeyCount = sources.Length + rebuildOperations
            .Select(static operation => operation.ResourceId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (freshKeys.Count != expectedFreshKeyCount || freshKeys.Distinct(StringComparer.Ordinal).Count() != freshKeys.Count)
        {
            throw new InvalidOperationException("The constrained rebuild write set is incomplete or contains a logical key collision.");
        }

        IReadOnlyList<string> baselineKeys = BaselineProjectionKeys(baselinePartitionTenant, baselineEvidence);
        await AssertPartitionAbsentAsync(readModelEraser, freshKeys, scenarioToken).ConfigureAwait(false);
        IReadOnlyDictionary<string, string> baselineEtagsBefore = await ReadEtagsAsync(
            readModelEraser,
            baselineKeys,
            scenarioToken).ConfigureAwait(false);

        DateTimeOffset startedAtUtc = clock.UtcNow;
        Stopwatch timer = Stopwatch.StartNew();
        ProjectionRebuildMeasurement? result = null;
        Exception? primaryFailure = null;
        try
        {
            await RebuildPartitionThroughRealHandlerAsync(
                readModelStore,
                clock,
                freshPartitionTenant,
                sources,
                rebuildOperations,
                scenarioToken).ConfigureAwait(false);
            IReadOnlyList<ProjectionResourceDigest> rebuilt = await ReadRebuiltSnapshotAsync(
                readModelStore,
                freshPartitionTenant,
                sources,
                rebuildOperations,
                scenarioToken).ConfigureAwait(false);
            string rebuiltSchemaVersion = SnapshotSchemaVersion(
                ProjectConversationSourceEmailView.CurrentSchemaVersion,
                GovernedOperationView.CurrentSchemaVersion);
            IReadOnlyDictionary<string, string> baselineEtagsAfter = await ReadEtagsAsync(
                readModelEraser,
                baselineKeys,
                scenarioToken).ConfigureAwait(false);
            bool baselinePartitionUntouched = BaselineEtagsUnchanged(baselineEtagsBefore, baselineEtagsAfter);
            timer.Stop();
            bool sourcesEquivalent = rebuilt.SequenceEqual(baseline);
            result = new ProjectionRebuildMeasurement(
                startedAtUtc,
                clock.UtcNow,
                timer.Elapsed,
                baseline,
                rebuilt,
                preRebuildSchemaVersion,
                rebuiltSchemaVersion,
                new RecoveryValidationExecutionAssertions(
                    CleanupComplete: false, // patched below with the cleanup step's real, independently observed outcome
                    FaultObserved: false,
                    RecoveryObserved: rebuilt.Count > 0,
                    IndependentControlSucceeded: false, // not observed in this driver
                    TenantIsolationPreserved: baselinePartitionUntouched,
                    UnauthorizedMutationAbsent: false, // not observed — do not fabricate
                    StateReconstructable: sourcesEquivalent,
                    // The rebuild consumes only its independently loaded immutable source dataset and WORM store.
                    ImmutableSourceOnly: true,
                    MailboxReingestionAbsent: true),
                SourceResourceCount: sources.Length,
                GovernedResourceCount: rebuildOperations.Select(static operation => operation.ResourceId).Distinct(StringComparer.Ordinal).Count(),
                WormRecordCount: auditRecords.Count,
                WormOperationCount: rebuildOperations.Count);
        }
        catch (Exception exception)
        {
            timer.Stop();
            primaryFailure = exception;
        }

        // Task 4: do not destroy failed-partition evidence before capture. Skip erase when rebuild failed.
        // The Aspire E2E owns post-capture compensating erase (Decision 2 option 1).
        if (primaryFailure is not null)
        {
            ExceptionDispatchInfo.Capture(primaryFailure).Throw();
        }

        Exception? cleanupFailure = null;
        bool cleanupComplete = false;
        try
        {
            await ErasePartitionAsync(readModelEraser, freshKeys, options.RestorationTimeout)
                .ConfigureAwait(false);
            cleanupComplete = true;
        }
        catch (Exception exception)
        {
            cleanupFailure = exception;
        }

        if (cleanupFailure is not null)
        {
            ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
        }

        ProjectionRebuildMeasurement measurement = result
            ?? throw new InvalidOperationException("Projection rebuild completed without a measurement.");
        return measurement with { ExecutionAssertions = measurement.ExecutionAssertions! with { CleanupComplete = cleanupComplete } };
    }

    /// <summary>
    /// Seeds the versioned pre-rebuild baseline through the same production read-model projection stores used by the
    /// live rebuild. The seed is deliberately outside <see cref="RebuildAsync"/> so the rebuild cannot manufacture both
    /// sides of its own equivalence comparison.
    /// </summary>
    internal static async Task<ProjectionRebuildBaselineEvidence> SeedBaselineAsync(
        IReadModelStore store,
        string testTenantRef,
        RecoveryValidationDatasetDescriptor descriptor,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormAuditChainRecord> auditRecords,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(auditRecords);
        if (!ReplayTenantPolicy.IsTestTenant(testTenantRef) ||
            sources.Any(source => !string.Equals(source.TenantId, testTenantRef, StringComparison.Ordinal)) ||
            auditRecords.Any(record => !string.Equals(record.Envelope.TenantId, testTenantRef, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Projection baseline seeding is restricted to one replay-test tenant.");
        }

        if (sources.Select(static source => source.IntakeId).Distinct(StringComparer.Ordinal).Count() != sources.Count ||
            sources.Any(static source => !AuditMetadata.IsSafeStableIdentifier(source.IntakeId)))
        {
            throw new InvalidOperationException("Projection baseline sources contain an unsafe or duplicate logical resource id.");
        }

        IReadOnlyList<WormOperationGroup> operations = GroupOperations(auditRecords);
        string partitionTenant = BaselinePartitionTenant(testTenantRef, descriptor.ValidationPartitionRef);
        var sourceStore = new ReadModelProjectConversationProjectionStore(store);
        var governedStore = new ReadModelGovernedOperationViewStore(store);
        foreach (ProjectConversationSourceEmailView source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await sourceStore
                .UpsertSourceEmailAsync(source with { TenantId = partitionTenant }, cancellationToken)
                .ConfigureAwait(false);
        }

        Dictionary<string, string> governedHistoryTokens = new(StringComparer.Ordinal);
        foreach (WormOperationGroup operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AuditEnvelope representative = BaselineResultBearingEnvelope(operation.Records);
            long sourceVersion = checked(operation.Records.Max(static record => record.Sequence) + 1);
            await governedStore
                .SaveAsync(
                    new GovernedOperationView(
                        partitionTenant,
                        operation.ResourceId,
                        GovernedOperationView.CurrentSchemaVersion,
                        GovernedOperationView.GovernedCommandProvenance,
                        GovernedOperationView.CurrentDerivationKernelVersion,
                        BaselineProjectionRedactionState(representative),
                        GovernedOperationView.GovernedOperationalRetentionClass,
                        sourceVersion,
                        representative.Timestamp,
                        representative.Timestamp),
                    cancellationToken)
                .ConfigureAwait(false);

            string historyToken = BaselineOperationHistoryToken(operation, representative);
            if (governedHistoryTokens.TryGetValue(operation.ResourceId, out string? existing))
            {
                governedHistoryTokens[operation.ResourceId] = Digest(existing, historyToken);
            }
            else
            {
                governedHistoryTokens[operation.ResourceId] = historyToken;
            }
        }

        return new ProjectionRebuildBaselineEvidence(
            [.. sources.Select(static source => source.IntakeId).Order(StringComparer.Ordinal)],
            governedHistoryTokens,
            SnapshotSchemaVersion(ProjectConversationSourceEmailView.CurrentSchemaVersion, GovernedOperationView.CurrentSchemaVersion),
            auditRecords.Count,
            operations.Count);
    }

    /// <summary>
    /// Rebuilds the fresh partition by reconstructing each immutable source into a <see cref="MailboxMessageIntakeCaptured"/>
    /// event and replaying it through the real <see cref="AssociationProjectionHandler"/> — the same production code that
    /// turns a captured mailbox event into a <see cref="ProjectConversationSourceEmailView"/> — instead of copying the
    /// pre-existing view verbatim. A real handler regression (or a deliberately mutated reconstruction) is therefore a
    /// reachable divergence rather than a tautology of the same object compared against itself.
    /// </summary>
    private static async Task RebuildPartitionThroughRealHandlerAsync(
        IReadModelStore store,
        ISystemClock clock,
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormOperationGroup> operations,
        CancellationToken cancellationToken)
    {
        AssociationProjectionHandler handler = new(
            new ReadModelAssociationProjectionStore(store),
            clock,
            new ReadModelProjectConversationProjectionStore(store));
        foreach (ProjectConversationSourceEmailView source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AssociationProjectionHandler.ProjectionOutcome outcome = await handler
                .HandleAsync(
                    ReconstructCapturedEvent(source),
                    partitionTenant,
                    source.SourceVersion,
                    source.CorrelationId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (outcome != AssociationProjectionHandler.ProjectionOutcome.Applied)
            {
                throw new InvalidOperationException(
                    "The real projection handler ignored a reconstructed rebuild source instead of applying it.");
            }
        }

        var governedStore = new ReadModelGovernedOperationViewStore(store);
        GovernedOperationProjectionHandler governedHandler = new(governedStore, clock);
        foreach (WormOperationGroup operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AuditOperationReconstructionResult reconstruction = AuditOperationReconstructor.Reconstruct(
                [.. operation.Records.Select(static record => record.Envelope)]);
            if (!reconstruction.IsReconstructable || reconstruction.State is not { } state ||
                !string.Equals(state.ResourceId, operation.ResourceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A grouped WORM operation could not be reconstructed through the production audit path.");
            }

            WormAuditChainRecord resultRecord = ResultBearingRecord(operation.Records);
            long sourceVersion = checked(operation.Records.Max(static record => record.Sequence) + 1);
            GovernedOperationProjectionHandler.ProjectionOutcome outcome = await governedHandler
                .HandleAsync(
                    new GovernedNoteRecordedNotification(
                        partitionTenant,
                        state.ResourceId,
                        operation.CorrelationId,
                        sourceVersion,
                        resultRecord.Envelope.Timestamp,
                        operation.CorrelationId),
                    cancellationToken)
                .ConfigureAwait(false);
            if (outcome != GovernedOperationProjectionHandler.ProjectionOutcome.Applied)
            {
                throw new InvalidOperationException("The production governed projection handler ignored a grouped WORM operation.");
            }
        }
    }

    /// <summary>
    /// Reverses the metadata-only projection back into the captured-event shape the real handler expects. Sender,
    /// recipients, and attachment content are never retained by the redacted immutable view, so reconstruction uses
    /// safe placeholders for those fields. Every safely retainable persisted structural field included by the snapshot
    /// digest round-trips exactly; participant identities remain deliberately outside the metadata-only evidence.
    /// </summary>
    private static MailboxMessageIntakeCaptured ReconstructCapturedEvent(ProjectConversationSourceEmailView source)
        => new(
            source.IntakeId,
            source.SourceProviderMessageId,
            source.InternetMessageId ?? string.Empty,
            source.SourceConversationId,
            source.SourceThreadId,
            source.SourceMailboxId,
            new MailboxParticipantIdentity("rebuild-reconstruction@invalid", DisplayName: null),
            [],
            source.SourceReceivedAtUtc,
            source.SourceSentAtUtc,
            source.SourceCreatedAtUtc,
            [],
            source.SourceTimezone,
            "rebuild-reconstruction",
            source.SourceProvenance,
            "rebuild-reconstruction-kernel-v1",
            source.RedactionState,
            source.RetentionClass,
            SchemaVersion: 1,
            source.Authenticity,
            source.DelegatedSender,
            source.ExternalSender);

    private static async Task<IReadOnlyList<ProjectionResourceDigest>> ReadBaselineSnapshotAsync(
        IReadModelStore store,
        string partitionTenant,
        ProjectionRebuildBaselineEvidence evidence,
        CancellationToken cancellationToken)
    {
        var sourceStore = new ReadModelProjectConversationProjectionStore(store);
        var governedStore = new ReadModelGovernedOperationViewStore(store);
        List<ProjectionResourceDigest> snapshot = [];
        foreach (string intakeId in evidence.SourceIntakeIds.Order(StringComparer.Ordinal))
        {
            ProjectConversationSourceEmailView source = await sourceStore
                .GetSourceEmailAsync(partitionTenant, intakeId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The persisted source-email projection was missing from the validation partition.");
            snapshot.Add(SourceDigest(source));
        }

        foreach ((string resourceId, string historyToken) in evidence.GovernedHistoryTokens.OrderBy(static item => item.Key, StringComparer.Ordinal))
        {
            GovernedOperationView view = await governedStore
                .GetAsync(partitionTenant, resourceId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The persisted audit-derived projection was missing from the validation partition.");
            snapshot.Add(GovernedDigest(view, historyToken));
        }

        return [.. snapshot.OrderBy(static digest => digest.ResourceId, StringComparer.Ordinal)];
    }

    private static async Task<IReadOnlyList<ProjectionResourceDigest>> ReadRebuiltSnapshotAsync(
        IReadModelStore store,
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormOperationGroup> operations,
        CancellationToken cancellationToken)
    {
        var sourceStore = new ReadModelProjectConversationProjectionStore(store);
        var governedStore = new ReadModelGovernedOperationViewStore(store);
        List<ProjectionResourceDigest> snapshot = [];
        foreach (ProjectConversationSourceEmailView expected in sources.OrderBy(static source => source.IntakeId, StringComparer.Ordinal))
        {
            ProjectConversationSourceEmailView source = await sourceStore
                .GetSourceEmailAsync(partitionTenant, expected.IntakeId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The persisted source-email projection was missing from the validation partition.");
            snapshot.Add(SourceDigest(source));
        }

        foreach (IGrouping<string, WormOperationGroup> resourceHistory in operations
            .GroupBy(static operation => operation.ResourceId, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            GovernedOperationView view = await governedStore
                .GetAsync(partitionTenant, resourceHistory.Key, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The persisted audit-derived projection was missing from the validation partition.");
            string historyToken = string.Empty;
            foreach (WormOperationGroup operation in resourceHistory.OrderBy(static item => item.Records[0].Sequence))
            {
                AuditOperationReconstructionResult reconstruction = AuditOperationReconstructor.Reconstruct(
                    [.. operation.Records.Select(static record => record.Envelope)]);
                if (!reconstruction.IsReconstructable || reconstruction.State is not { } state)
                {
                    throw new InvalidOperationException("A grouped WORM operation could not be reconstructed for snapshot evidence.");
                }

                string operationToken = RebuiltOperationHistoryToken(operation, state);
                historyToken = historyToken.Length == 0 ? operationToken : Digest(historyToken, operationToken);
            }

            snapshot.Add(GovernedDigest(view, historyToken));
        }

        return [.. snapshot.OrderBy(static digest => digest.ResourceId, StringComparer.Ordinal)];
    }

    private static ProjectionResourceDigest SourceDigest(ProjectConversationSourceEmailView source)
    {
        List<string> values =
        [
            source.SchemaVersion,
            source.SourceVersion.ToString(CultureInfo.InvariantCulture),
            source.SourceMailboxId,
            source.SourceProviderMessageId,
            source.InternetMessageId ?? "absent",
            source.SourceConversationId,
            source.SourceThreadId ?? "absent",
            source.SourceReceivedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            source.SourceSentAtUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "absent",
            source.SourceCreatedAtUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "absent",
            source.SourceTimezone ?? "absent",
            source.SourceProvenanceDisplayToken,
            source.SourceProvenance,
            source.RedactionState,
            source.RetentionClass,
            source.CorrelationId,
        ];
        AppendAuthenticityFacts(values, source.Authenticity);
        AppendDelegatedSenderFacts(values, source.DelegatedSender);
        AppendExternalSenderFacts(values, source.ExternalSender);
        return ProjectionResourceDigest.Create($"source-{source.IntakeId}", Digest([.. values]));
    }

    private static ProjectionResourceDigest GovernedDigest(GovernedOperationView view, string historyToken)
    {
        return ProjectionResourceDigest.Create(
            $"governed-{view.NoteId}",
            Digest(
                view.SchemaVersion,
                view.SourceVersion.ToString(CultureInfo.InvariantCulture),
                view.SourceProvenance,
                view.DerivationKernelVersion,
                view.RedactionState,
                view.RetentionClass,
                historyToken));
    }

    private static void AppendAuthenticityFacts(List<string> values, MailboxAuthenticityMetadata? authenticity)
    {
        if (authenticity is null)
        {
            values.Add("authenticity-absent");
            return;
        }

        MailboxAuthenticationResultSnapshot results = authenticity.AuthenticationResults;
        values.AddRange(
        [
            "authenticity-present",
            results.Spf.ToString(),
            results.Dkim.ToString(),
            results.Dmarc.ToString(),
            results.CompositeAuthentication.ToString(),
            results.CompositeAuthenticationReason ?? "absent",
        ]);
        AppendHeaders(values, results.AuthenticationResultsHeaders);
        MailboxHeaderInspectionSnapshot inspection = authenticity.HeaderInspection;
        AppendHeaders(values, inspection.ReceivedHeaders);
        AppendHeaders(values, inspection.AuthenticationResultsHeaders);
        values.AddRange(
        [
            inspection.From.ToString(),
            inspection.ReplyTo.ToString(),
            inspection.Sender.ToString(),
            inspection.XOriginalSender.ToString(),
            .. inspection.Discrepancies.Select(static item => item.ToString()),
        ]);
        if (authenticity.StrictnessPolicy is { } policy)
        {
            values.AddRange(["strictness-present", policy.Strictness.ToString(), policy.PolicyVersion, policy.ReasonCode]);
        }
        else
        {
            values.Add("strictness-absent");
        }
    }

    private static void AppendHeaders(List<string> values, IReadOnlyList<MailboxSelectedHeaderSnapshot> headers)
    {
        values.Add(headers.Count.ToString(CultureInfo.InvariantCulture));
        foreach (MailboxSelectedHeaderSnapshot header in headers.OrderBy(static item => item.Ordinal))
        {
            values.AddRange([header.Name, header.Ordinal.ToString(CultureInfo.InvariantCulture), header.ValueState.ToString()]);
        }
    }

    private static void AppendDelegatedSenderFacts(List<string> values, MailboxDelegatedSenderSnapshot? delegated)
    {
        if (delegated is null)
        {
            values.Add("delegated-sender-absent");
            return;
        }

        // Participant identities may contain addresses/display names and are intentionally not retained, even hashed.
        values.AddRange(
        [
            "delegated-sender-present",
            delegated.State.ToString(),
            delegated.Delegate is null ? "delegate-absent" : "delegate-present",
            delegated.PrincipalFor is null ? "principal-absent" : "principal-present",
            .. delegated.EvidenceRefs,
            .. delegated.Discrepancies.Select(static item => item.ToString()),
        ]);
    }

    private static void AppendExternalSenderFacts(List<string> values, MailboxExternalSenderPosture? external)
    {
        if (external is null)
        {
            values.Add("external-sender-absent");
            return;
        }

        values.AddRange(
        [
            "external-sender-present",
            external.ExternalSender.ToString(CultureInfo.InvariantCulture),
            external.PartyResolutionState.ToString(),
            external.ResolvedPartyRef ?? "absent",
            .. external.EvidenceRefs,
        ]);
    }

    private static IReadOnlyList<WormOperationGroup> GroupOperations(IReadOnlyList<WormAuditChainRecord> records)
    {
        if (records.Count == 0 || records.Any(static record => record is null) ||
            records.Select(static record => record.Sequence).Distinct().Count() != records.Count)
        {
            throw new InvalidOperationException("The selected WORM chain is empty, malformed, or has duplicate sequences.");
        }

        foreach (WormAuditChainRecord record in records)
        {
            if (!AuditMetadata.IsSafeStableIdentifier(record.Envelope.ResourceId) ||
                !AuditMetadata.IsSafeStableIdentifier(record.Envelope.CorrelationId))
            {
                throw new InvalidOperationException("The selected WORM chain contains an unsafe logical resource or operation id.");
            }
        }

        return
        [
            .. records
                .GroupBy(static record => (record.Envelope.ResourceId, record.Envelope.CorrelationId))
                .Select(static group => new WormOperationGroup(
                    group.Key.ResourceId,
                    group.Key.CorrelationId,
                    [.. group.OrderBy(static record => record.Sequence)]))
                .OrderBy(static operation => operation.Records[0].Sequence)
                .ThenBy(static operation => operation.ResourceId, StringComparer.Ordinal)
                .ThenBy(static operation => operation.CorrelationId, StringComparer.Ordinal),
        ];
    }

    private static string BaselineOperationHistoryToken(WormOperationGroup operation, AuditEnvelope representative)
    {
        ChatBotStateWritingPath path = ChatBotAuditPathMap.Resolve(representative)
            ?? throw new InvalidOperationException("The independent baseline WORM operation maps to no governed state-writing path.");
        string projectionRedactionState = BaselineProjectionRedactionState(representative);
        return Digest(
        [
            .. ChainFacts(operation.Records),
            representative.ResourceId,
            representative.Decision,
            representative.ReasonCode,
            representative.PolicySnapshotId,
            representative.StateTransition,
            representative.Outcome,
            projectionRedactionState,
            $"{path.Code}:{representative.StateTransition}:{representative.Outcome}",
        ]);
    }

    private static string RebuiltOperationHistoryToken(WormOperationGroup operation, ReconstructedOperationState state)
        => Digest(
        [
            .. ChainFacts(operation.Records),
            state.ResourceId,
            state.Decision,
            state.ReasonCode,
            state.PolicySnapshotId,
            state.StateTransition,
            state.Outcome,
            state.ProjectionRedactionState,
            state.ResultingStateToken,
        ]);

    private static IEnumerable<string> ChainFacts(IReadOnlyList<WormAuditChainRecord> records)
    {
        foreach (WormAuditChainRecord record in records.OrderBy(static item => item.Sequence))
        {
            AuditEnvelope envelope = record.Envelope;
            yield return record.Sequence.ToString(CultureInfo.InvariantCulture);
            yield return record.PredecessorHash;
            yield return record.RecordHash;
            yield return record.CanonicalSerializationVersion;
            yield return envelope.EnvelopeSchemaVersion;
            yield return envelope.Phase.ToString();
            yield return envelope.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            yield return envelope.ActorId;
            yield return envelope.ActorType;
            yield return envelope.CommandName;
            yield return envelope.ResourceId;
            yield return envelope.CorrelationId;
            yield return envelope.Decision;
            yield return envelope.ReasonCode;
            yield return envelope.PolicySnapshotId;
            yield return envelope.IdempotencyKey ?? "absent";
            yield return envelope.StateTransition;
            yield return envelope.RedactionDecision;
            yield return envelope.Outcome;
            yield return envelope.SurfaceOrigin;
            yield return envelope.ReplayRunId ?? "absent";
            yield return envelope.SourceEvidenceRefs.Count.ToString(CultureInfo.InvariantCulture);
            foreach (string evidenceRef in envelope.SourceEvidenceRefs)
            {
                yield return evidenceRef;
            }
        }
    }

    private static AuditEnvelope BaselineResultBearingEnvelope(IReadOnlyList<WormAuditChainRecord> records)
        => ResultBearingRecord(records).Envelope;

    private static WormAuditChainRecord ResultBearingRecord(IReadOnlyList<WormAuditChainRecord> records)
        => records.LastOrDefault(static record => record.Envelope.Phase == AuditCommitPhase.PostCommit) ?? records[^1];

    private static string BaselineProjectionRedactionState(AuditEnvelope envelope)
        => string.Equals(envelope.RedactionDecision, CoarseUserFacingRedactionStage.MetadataOnlyDecision, StringComparison.Ordinal)
            ? GovernedOperationView.MetadataOnlyRedactionState
            : envelope.RedactionDecision;

    private static string SnapshotSchemaVersion(string sourceSchemaVersion, string governedSchemaVersion)
        => $"{sourceSchemaVersion}|{governedSchemaVersion}";

    private static async Task<IReadOnlyDictionary<string, string>> ReadEtagsAsync(
        IReadModelConditionalEraser eraser,
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> etags = new(StringComparer.Ordinal);
        foreach (string key in keys)
        {
            (bool present, string etag) = await eraser
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
                .ConfigureAwait(false);
            if (present)
            {
                etags[key] = etag;
            }
        }

        return etags;
    }

    private static bool BaselineEtagsUnchanged(
        IReadOnlyDictionary<string, string> before,
        IReadOnlyDictionary<string, string> after)
    {
        if (before.Count != after.Count)
        {
            return false;
        }

        foreach ((string key, string etag) in before)
        {
            if (!after.TryGetValue(key, out string? later) || !string.Equals(etag, later, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Builds the persisted keys for a rebuild or baseline partition (shared with the Aspire E2E cleanup).</summary>
    internal static IReadOnlyList<string> ProjectionKeys(
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormAuditChainRecord> auditRecords)
        =>
        [
            .. sources.Select(source => ProjectConversationSourceEmailView.KeyFor(partitionTenant, source.IntakeId)),
            .. auditRecords
                .Select(static record => record.Envelope.ResourceId)
                .Distinct(StringComparer.Ordinal)
                .Select(resourceId => GovernedOperationView.KeyFor(partitionTenant, resourceId)),
        ];

    private static IReadOnlyList<string> BaselineProjectionKeys(
        string partitionTenant,
        ProjectionRebuildBaselineEvidence evidence)
        =>
        [
            .. evidence.SourceIntakeIds.Select(intakeId => ProjectConversationSourceEmailView.KeyFor(partitionTenant, intakeId)),
            .. evidence.GovernedHistoryTokens.Keys.Select(resourceId => GovernedOperationView.KeyFor(partitionTenant, resourceId)),
        ];

    /// <summary>Fresh-partition tenant label used by the live rebuild driver and post-capture E2E erase.</summary>
    internal static string FreshPartitionTenant(string testTenantRef, string validationPartitionRef, string correlationId)
        => $"{testTenantRef}:rebuild:{validationPartitionRef}:{correlationId}";

    /// <summary>
    /// Erases a rebuild partition after evidence capture. Used by the success-path driver cleanup and by the Aspire
    /// E2E compensating <c>finally</c> when Task 4 skipped erase on failure.
    /// </summary>
    internal static async Task ErasePartitionAsync(
        IReadModelConditionalEraser eraser,
        IReadOnlyList<string> keys,
        TimeSpan timeout)
    {
        // Deliberately NOT linked to the caller's cancellation token — mirroring the continuity/scoped-outage
        // drivers' independent restoration CTS — so a cancelled workflow still gets cleanup's own full budget
        // instead of the fresh partition being stranded the instant the caller's token cancels.
        using CancellationTokenSource cleanupDeadline = new(timeout);
        foreach (string key in keys)
        {
            (bool present, string etag) = await eraser
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, cleanupDeadline.Token)
                .ConfigureAwait(false);
            if (present && !await eraser
                .TryEraseAsync(ChatBotReadModelStoreNames.StateStoreName, key, etag, cleanupDeadline.Token)
                .ConfigureAwait(false))
            {
                throw new InvalidOperationException("The fresh projection rebuild partition changed during cleanup.");
            }

            (bool remains, _) = await eraser
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, cleanupDeadline.Token)
                .ConfigureAwait(false);
            if (remains)
            {
                throw new InvalidOperationException("The fresh projection rebuild partition remained after cleanup.");
            }
        }
    }

    private static async Task AssertPartitionAbsentAsync(
        IReadModelConditionalEraser eraser,
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        foreach (string key in keys)
        {
            (bool present, _) = await eraser
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
                .ConfigureAwait(false);
            if (present)
            {
                throw new InvalidOperationException("The fresh projection rebuild partition was not empty before execution.");
            }
        }
    }

    private static string BaselinePartitionTenant(string testTenantRef, string validationPartitionRef)
        => $"{testTenantRef}:baseline:{validationPartitionRef}";

    private static string Digest(params string[] values)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] length = new byte[sizeof(int)];
        foreach (string value in values)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
