using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Audit;
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

        IReadOnlyList<WormAuditChainRecord> auditRecords = wormAuditStore.EnumerateChain(testTenantRef);
        if (auditRecords.Count != dataset.WormAuditRecordCount)
        {
            throw new InvalidOperationException("The selected tenant WORM population does not match the dataset.");
        }

        string baselinePartitionTenant = BaselinePartitionTenant(testTenantRef, dataset.ValidationPartitionRef);
        IReadOnlyList<ProjectionResourceDigest> baselineFull = await ReadSnapshotAsync(
            readModelStore,
            baselinePartitionTenant,
            sources,
            auditRecords,
            scenarioToken).ConfigureAwait(false);
        // Claim narrowing (decision 2.2): structural proof is source-email digests only. WORM/governed identity-writes
        // remain as RV-REBUILD-WORM residual and are excluded from equivalence.
        IReadOnlyList<ProjectionResourceDigest> baseline = SourceDigestsOnly(baselineFull);
        string preRebuildSchemaVersion = await ReadObservedSourceSchemaAsync(
            readModelStore,
            baselinePartitionTenant,
            sources,
            scenarioToken).ConfigureAwait(false);
        string freshPartitionTenant = FreshPartitionTenant(testTenantRef, dataset.ValidationPartitionRef, correlationId);
        IReadOnlyList<string> freshKeys = ProjectionKeys(freshPartitionTenant, sources, auditRecords);
        IReadOnlyList<string> baselineKeys = ProjectionKeys(baselinePartitionTenant, sources, auditRecords);
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
                auditRecords,
                scenarioToken).ConfigureAwait(false);
            IReadOnlyList<ProjectionResourceDigest> rebuiltFull = await ReadSnapshotAsync(
                readModelStore,
                freshPartitionTenant,
                sources,
                auditRecords,
                scenarioToken).ConfigureAwait(false);
            IReadOnlyList<ProjectionResourceDigest> rebuilt = SourceDigestsOnly(rebuiltFull);
            string rebuiltSchemaVersion = await ReadObservedSourceSchemaAsync(
                readModelStore,
                freshPartitionTenant,
                sources,
                scenarioToken).ConfigureAwait(false);
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
                    // Honest for the source path: rebuild uses only immutableSourceRecords + AssociationProjectionHandler.
                    ImmutableSourceOnly: true,
                    MailboxReingestionAbsent: true));
        }
        catch (Exception exception)
        {
            timer.Stop();
            primaryFailure = exception;
        }

        // Task 4: do not destroy failed-partition evidence before capture. Skip erase when rebuild failed.
        if (primaryFailure is not null)
        {
            ExceptionDispatchInfo.Capture(primaryFailure).Throw();
        }

        Exception? cleanupFailure = null;
        bool cleanupComplete = false;
        try
        {
            await CleanupPartitionAsync(readModelEraser, freshKeys, options.RestorationTimeout)
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
    internal static async Task SeedBaselineAsync(
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

        await ProjectPartitionAsync(
            store,
            BaselinePartitionTenant(testTenantRef, descriptor.ValidationPartitionRef),
            sources,
            auditRecords,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task ProjectPartitionAsync(
        IReadModelStore store,
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormAuditChainRecord> auditRecords,
        CancellationToken cancellationToken)
    {
        var sourceStore = new ReadModelProjectConversationProjectionStore(store);
        var governedStore = new ReadModelGovernedOperationViewStore(store);
        foreach (ProjectConversationSourceEmailView source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await sourceStore
                .UpsertSourceEmailAsync(source with { TenantId = partitionTenant }, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (WormAuditChainRecord auditRecord in auditRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await governedStore
                .SaveAsync(ToGovernedOperationView(partitionTenant, auditRecord), cancellationToken)
                .ConfigureAwait(false);
        }
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
        IReadOnlyList<WormAuditChainRecord> auditRecords,
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

        // WORM/governed projections remain identity-written (RV-REBUILD-WORM). They are persisted for partition
        // completeness but excluded from structural equivalence claims above.
        var governedStore = new ReadModelGovernedOperationViewStore(store);
        foreach (WormAuditChainRecord auditRecord in auditRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await governedStore
                .SaveAsync(ToGovernedOperationView(partitionTenant, auditRecord), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reverses the metadata-only projection back into the captured-event shape the real handler expects. Sender,
    /// recipients, and attachment content are never retained by the redacted immutable view, so reconstruction uses
    /// safe placeholders for those fields — the fields the story's structural digest and equivalence checks actually
    /// compare (schema version, source version, provenance, redaction state, retention class) all round-trip exactly.
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

    private static async Task<IReadOnlyList<ProjectionResourceDigest>> ReadSnapshotAsync(
        IReadModelStore store,
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormAuditChainRecord> auditRecords,
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
            snapshot.Add(ProjectionResourceDigest.Create(
                $"source-{source.IntakeId}",
                Digest(
                    source.SchemaVersion,
                    source.SourceVersion.ToString(CultureInfo.InvariantCulture),
                    source.SourceProvenance,
                    source.RedactionState,
                    source.RetentionClass)));
        }

        foreach (WormAuditChainRecord expected in auditRecords.OrderBy(static record => record.Sequence))
        {
            GovernedOperationView view = await governedStore
                .GetAsync(partitionTenant, expected.Envelope.ResourceId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The persisted audit-derived projection was missing from the validation partition.");
            snapshot.Add(ProjectionResourceDigest.Create(
                $"worm-{expected.Sequence.ToString(CultureInfo.InvariantCulture)}",
                Digest(
                    view.SchemaVersion,
                    view.SourceVersion.ToString(CultureInfo.InvariantCulture),
                    view.SourceProvenance,
                    view.DerivationKernelVersion,
                    view.RedactionState,
                    view.RetentionClass)));
        }

        return snapshot;
    }

    private static IReadOnlyList<ProjectionResourceDigest> SourceDigestsOnly(IReadOnlyList<ProjectionResourceDigest> snapshot)
        => snapshot
            .Where(digest => digest.ResourceId.StartsWith("source-", StringComparison.Ordinal))
            .ToArray();

    private static async Task<string> ReadObservedSourceSchemaAsync(
        IReadModelStore store,
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        CancellationToken cancellationToken)
    {
        var sourceStore = new ReadModelProjectConversationProjectionStore(store);
        ProjectConversationSourceEmailView first = await sourceStore
            .GetSourceEmailAsync(partitionTenant, sources[0].IntakeId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The persisted source-email projection was missing from the validation partition.");
        return first.SchemaVersion;
    }

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

    private static GovernedOperationView ToGovernedOperationView(string partitionTenant, WormAuditChainRecord record)
    {
        AuditOperationReconstructionResult reconstruction = AuditOperationReconstructor.Reconstruct([record.Envelope]);
        if (!reconstruction.IsReconstructable || reconstruction.State is not { } state)
        {
            throw new InvalidOperationException("The selected WORM record could not be rebuilt into a projection state.");
        }

        return new GovernedOperationView(
            partitionTenant,
            state.ResourceId,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            state.ProjectionRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            record.Sequence,
            record.Envelope.Timestamp,
            record.Envelope.Timestamp);
    }

    private static IReadOnlyList<string> ProjectionKeys(
        string partitionTenant,
        IReadOnlyList<ProjectConversationSourceEmailView> sources,
        IReadOnlyList<WormAuditChainRecord> auditRecords)
        =>
        [
            .. sources.Select(source => ProjectConversationSourceEmailView.KeyFor(partitionTenant, source.IntakeId)),
            .. auditRecords.Select(record => GovernedOperationView.KeyFor(partitionTenant, record.Envelope.ResourceId)),
        ];

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

    private static async Task CleanupPartitionAsync(
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

    private static string BaselinePartitionTenant(string testTenantRef, string validationPartitionRef)
        => $"{testTenantRef}:baseline:{validationPartitionRef}";

    private static string FreshPartitionTenant(string testTenantRef, string validationPartitionRef, string correlationId)
        => $"{testTenantRef}:rebuild:{validationPartitionRef}:{correlationId}";

    private static string Digest(params string[] values)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', values)))).ToLowerInvariant();
}
