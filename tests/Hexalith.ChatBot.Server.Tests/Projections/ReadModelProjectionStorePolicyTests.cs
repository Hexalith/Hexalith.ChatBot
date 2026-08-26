using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ReadModelProjectionStorePolicyTests
{
    [Fact]
    public async Task GovernedOperationReadModelStoreShouldRetryOptimisticConflictsOnTenantScopedKey()
    {
        ConflictingReadModelStore store = new();
        ReadModelGovernedOperationViewStore projectionStore = new(store);
        GovernedOperationView view = new(
            "tenant-alpha",
            "note-alpha",
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            GovernedOperationView.MetadataOnlyRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            2,
            new DateTimeOffset(2026, 6, 12, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 12, 9, 1, 0, TimeSpan.Zero));

        await projectionStore.SaveAsync(view, TestContext.Current.CancellationToken).ConfigureAwait(true);

        string expectedKey = GovernedOperationView.KeyFor("tenant-alpha", "note-alpha");
        store.TrySaveCalls.ShouldBe(2);
        store.LastKey.ShouldBe(expectedKey);
        store.LastStoreName.ShouldBe(ChatBotReadModelStoreNames.StateStoreName);
        GovernedOperationView persisted = (await projectionStore
            .GetAsync("tenant-alpha", "note-alpha", TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        persisted.SourceVersion.ShouldBe(2);
    }

    [Fact]
    public async Task ProjectConversationReadModelStoreShouldUseWritePolicyForTenantProjectIndexes()
    {
        ConflictingReadModelStore store = new();
        ReadModelProjectConversationProjectionStore projectionStore = new(store);
        ProjectConversationItemView item = ProjectConversationItem("item-alpha", 3);

        await projectionStore.UpsertAsync(item, TestContext.Current.CancellationToken).ConfigureAwait(true);

        ProjectConversationPage page = await projectionStore
            .ReadPageAsync("tenant-alpha", "project-alpha", null, 25, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        page.Items.ShouldHaveSingleItem().ItemId.ShouldBe("item-alpha");
        store.DirectSaveCalls.ShouldBe(0);
        store.TrySaveCalls.ShouldBeGreaterThan(1);
        store.SavedKeys.ShouldContain(ProjectConversationItemView.KeyFor("tenant-alpha", "project-alpha", "item-alpha"));
        store.SavedKeys.ShouldContain("tenant-alpha:project-conversation:project-alpha:index");
        store.SavedKeys.ShouldContain("tenant-alpha:project-conversation:projects:index");
        store.SavedKeys.ShouldContain("project-conversation:tenants:index");
    }

    [Fact]
    public async Task ProjectConversationReadModelStoreShouldPageNewestFirstWithStableEqualTimeCursor()
    {
        ConflictingReadModelStore store = new();
        ReadModelProjectConversationProjectionStore projectionStore = new(store);
        await projectionStore.UpsertAsync(ProjectConversationItem("item-a", 1), TestContext.Current.CancellationToken);
        await projectionStore.UpsertAsync(ProjectConversationItem("item-b", 2), TestContext.Current.CancellationToken);
        await projectionStore.UpsertAsync(ProjectConversationItem("item-c", 3), TestContext.Current.CancellationToken);

        ProjectConversationPage first = await projectionStore
            .ReadPageAsync("tenant-alpha", "project-alpha", null, 2, TestContext.Current.CancellationToken);
        ProjectConversationPage second = await projectionStore
            .ReadPageAsync("tenant-alpha", "project-alpha", first.NextCursorPosition, 2, TestContext.Current.CancellationToken);

        first.Items.Select(static item => item.ItemId).ShouldBe(["item-c", "item-b"]);
        first.HasMore.ShouldBeTrue();
        first.LatestItem.ShouldNotBeNull().ItemId.ShouldBe("item-c");
        second.Items.Select(static item => item.ItemId).ShouldBe(["item-a"]);
        second.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectConversationReadModelStoreShouldPublishOnlyPersistedAiProgressNudges()
    {
        ConflictingReadModelStore store = new();
        RecordingChangePublisher publisher = new();
        ReadModelProjectConversationProjectionStore projectionStore = new(store, publisher);
        AiOutcomeEventView progress = new(
            "tenant-alpha",
            "project-alpha",
            AiOutcomeKind.ExecutionStarted,
            AiOutcomeStatus.Executing,
            new DateTimeOffset(2026, 6, 12, 11, 0, 0, TimeSpan.Zero),
            4,
            "correlation-alpha",
            "ai-actor-alpha",
            "ai",
            ProposalId: "response-alpha",
            ExecutionStatus: "executing",
            AiResponseSequence: 1,
            AiResponseProgressState: "rendering",
            AiResponseVisibilityState: "metadata_only",
            AiResponseIsTerminal: false);
        AiOutcomeEventView proposal = progress with
        {
            OutcomeKind = AiOutcomeKind.Proposal,
            OutcomeStatus = AiOutcomeStatus.Proposed,
            SourceVersion = 5,
            AiResponseSequence = null,
            AiResponseProgressState = null,
            AiResponseVisibilityState = null,
            AiResponseIsTerminal = null,
        };

        await projectionStore.UpsertAiOutcomeEventAsync(progress, TestContext.Current.CancellationToken);
        await projectionStore.UpsertAiOutcomeEventAsync(proposal, TestContext.Current.CancellationToken);

        publisher.Tenants.ShouldBe(["tenant-alpha"]);
        ProjectConversationPage page = await projectionStore
            .ReadPageAsync("tenant-alpha", "project-alpha", null, 25, TestContext.Current.CancellationToken);
        page.Items.ShouldContain(static item => item.AiResponseProgressState == "rendering");
    }

    [Fact]
    public async Task ControlStateFreshnessRefreshShouldNotDowngradeConcurrentlyAdvancedRecord()
    {
        // The trusted snapshot is read first (active, v5). By the time the write policy re-reads under optimistic
        // concurrency, a concurrent control-state event has advanced the record to (disabled, v6). The refresh must
        // yield to that newer record rather than re-persisting the stale snapshot, which would downgrade the version
        // and reactivate a disabled subject.
        GovernedControlStateView trusted = ControlState(GovernedControlStateView.Active, sourceVersion: 5);
        GovernedControlStateView advanced = ControlState(GovernedControlStateView.Disabled, sourceVersion: 6);
        VersionAdvancingControlStateStore store = new(trusted, advanced);
        ReadModelGovernedControlStateProjectionStore projectionStore = new(store);

        bool refreshed = await projectionStore
            .TryRefreshFreshnessAsync(trusted, new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        refreshed.ShouldBeTrue();
        GovernedControlStateView persisted = store.LastSaved.ShouldNotBeNull();
        persisted.SourceVersion.ShouldBe(6);
        persisted.ControlState.ShouldBe(GovernedControlStateView.Disabled);
    }

    private static GovernedControlStateView ControlState(string controlState, long sourceVersion)
        => new(
            "tenant-alpha",
            GovernedControlSubjectClasses.MailboxSource,
            "subject-alpha",
            controlState,
            null,
            null,
            sourceVersion,
            "correlation-control",
            new DateTimeOffset(2026, 6, 12, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 12, 8, 0, 0, TimeSpan.Zero),
            false);

    private static ProjectConversationItemView ProjectConversationItem(string itemId, long sourceVersion)
        => new(
            "tenant-alpha",
            "project-alpha",
            "Project Alpha",
            itemId,
            "intake-alpha",
            ProjectConversationItemKind.EmailDerived,
            ProjectConversationActorKind.Mailbox,
            "Mailbox event",
            new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero),
            LifecycleState.Associated,
            AssociationThresholdBand.Auto,
            0.9,
            "association-alpha",
            "mailbox-alpha",
            null,
            null,
            "conversation-alpha",
            "thread-alpha",
            null,
            null,
            null,
            null,
            null,
            AssociationCandidateView.MailboxSourceProvenance,
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            sourceVersion,
            "correlation-alpha");

    private sealed class ConflictingReadModelStore : IReadModelStore
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _etags = new(StringComparer.Ordinal);
        private readonly HashSet<string> _conflictedKeys = new(StringComparer.Ordinal);

        public int TrySaveCalls { get; private set; }

        public int DirectSaveCalls { get; private set; }

        public string? LastKey { get; private set; }

        public string? LastStoreName { get; private set; }

        public IReadOnlyCollection<string> SavedKeys => _values.Keys;

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastStoreName = storeName;
            LastKey = key;
            return Task.FromResult(_values.TryGetValue(key, out object? value)
                ? new ReadModelEntry<TValue>((TValue)value, _etags[key])
                : new ReadModelEntry<TValue>(null, null));
        }

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            DirectSaveCalls++;
            _values[key] = value;
            _etags[key] = "etag-direct";
            LastStoreName = storeName;
            LastKey = key;
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrySaveCalls++;
            LastStoreName = storeName;
            LastKey = key;
            if (_conflictedKeys.Add(key))
            {
                return Task.FromResult(false);
            }

            string current = _etags.GetValueOrDefault(key, string.Empty);
            if (!string.Equals(current, etag, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _values[key] = value;
            _etags[key] = $"etag-{TrySaveCalls}";
            return Task.FromResult(true);
        }
    }

    private sealed class VersionAdvancingControlStateStore(
        GovernedControlStateView initial,
        GovernedControlStateView advanced) : IReadModelStore
    {
        private int _getCount;

        public GovernedControlStateView? LastSaved { get; private set; }

        public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
            string storeName,
            string key,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            _getCount++;
            GovernedControlStateView view = _getCount <= 1 ? initial : advanced;
            return Task.FromResult(new ReadModelEntry<TValue>((TValue)(object)view, $"etag-{_getCount}"));
        }

        public Task SaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSaved = (GovernedControlStateView)(object)value;
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync<TValue>(
            string storeName,
            string key,
            TValue value,
            string etag,
            CancellationToken cancellationToken = default)
            where TValue : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSaved = (GovernedControlStateView)(object)value;
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingChangePublisher : IProjectConversationChangePublisher
    {
        public List<string> Tenants { get; } = [];

        public Task PublishProjectConversationChangedAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Tenants.Add(tenantId);
            return Task.CompletedTask;
        }
    }
}
