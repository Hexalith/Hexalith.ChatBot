using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Verifies recovery inventory against rows persisted by the real production projection store.</summary>
public sealed class RecoveryIntakeInventoryTests
{
    private const string Tenant = "tenant-alpha";
    private const string Intake = "intake-001";
    private const string StoreName = ChatBotReadModelStoreNames.StateStoreName;

    [Fact]
    public async Task InventoryCoversProductionRootsAndMaterializedChildrenWithoutOwningSharedIndexes()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, Intake);
        IReadOnlyList<string> allKeys = store.Keys(StoreName);
        RecoveryIntakeReadModelProbe probe = new(store, store, TimeSpan.Zero);

        IReadOnlyList<string> inventory = await probe.CaptureKeysAsync(Tenant, Intake, TestContext.Current.CancellationToken);

        // Source, attachment set, three intake indexes, participant resolution and three conversation rows.
        inventory.Count.ShouldBe(9);
        inventory.Distinct(StringComparer.Ordinal).Count().ShouldBe(inventory.Count);
        inventory.All(allKeys.Contains).ShouldBeTrue();
        inventory.TakeLast(5).ShouldBe(RecoveryIntakeReadModelProbe.KeysFor(Tenant, Intake));
        string[] sharedKeys = ["project-conversation:tenants:index", $"{Tenant}:project-conversation:projects:index",
            $"{Tenant}:project-conversation:project-001:index"];
        allKeys.Except(inventory, StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .ShouldBe(sharedKeys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task CleanupPreservesAnotherIntakeAndTenantAndRetainsChildrenAfterTheirIndexesAreGone()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, "intake-002");
        await SeedAsync(store, "tenant-beta", Intake);
        await SeedAsync(store, Tenant, Intake);
        RecoveryIntakeReadModelProbe probe = new(store, store, TimeSpan.Zero);
        IReadOnlyList<string> inventory = await probe.CaptureKeysAsync(Tenant, Intake, TestContext.Current.CancellationToken);
        string[] preserved = store.Keys(StoreName).Except(inventory, StringComparer.Ordinal).ToArray();
        Dictionary<string, string> etags = new(StringComparer.Ordinal);
        foreach (string key in preserved)
        {
            etags.Add(key, (await store.TryReadEtagAsync(StoreName, key, TestContext.Current.CancellationToken)).Etag);
        }

        string child = inventory.First(key => !RecoveryIntakeReadModelProbe.KeysFor(Tenant, Intake).Contains(key));
        foreach (string key in inventory.Where(key => !string.Equals(key, child, StringComparison.Ordinal)))
        {
            await EraseAsync(store, key);
        }

        // A new inventory cannot rediscover the participant after its private intake index was deleted.
        // The original probe must still reject this residue after its discovery index is gone.
        (await probe.AreAbsentAsync(Tenant, Intake, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await probe.CaptureKeysAsync(Tenant, Intake, TestContext.Current.CancellationToken)).ShouldContain(child);
        await EraseAsync(store, child);
        (await probe.RemainsAbsentAsync(Tenant, Intake, TimeSpan.Zero, TestContext.Current.CancellationToken)).ShouldBeTrue();
        store.Keys(StoreName).ShouldBe(preserved.Order(StringComparer.Ordinal));
        foreach ((string key, string etag) in etags)
        {
            (await store.TryReadEtagAsync(StoreName, key, TestContext.Current.CancellationToken)).Etag.ShouldBe(etag);
        }
    }

    [Fact]
    public async Task AbsenceFindsMaterializedConversationResidueEvenWithoutIntakeIndexes()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, Intake);
        foreach (string key in RecoveryIntakeReadModelProbe.KeysFor(Tenant, Intake))
        {
            await EraseAsync(store, key);
        }

        RecoveryIntakeReadModelProbe fresh = new(store, store);
        (await fresh.AreAbsentAsync(Tenant, Intake, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task FailedOrCancelledInventoryPropagatesBeforeCleanupCanEraseAnything()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, Intake);
        IReadOnlyList<string> before = store.Keys(StoreName);
        RecoveryIntakeReadModelProbe probe = new(store, store);
        store.RejectReads = true;
        await Should.ThrowAsync<InvalidOperationException>(() => probe.CaptureKeysAsync(
            Tenant, Intake, TestContext.Current.CancellationToken));
        store.RejectReads = false;
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => probe.CaptureKeysAsync(Tenant, Intake, cancellation.Token));
        store.Keys(StoreName).ShouldBe(before);
        store.Erases.ShouldBe(0);
    }

    [Fact]
    public async Task CapturedParticipantReappearingAtTheClosingBoundaryIsNotLostWithItsIndex()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, Intake);
        RecoveryIntakeReadModelProbe probe = new(store, store, TimeSpan.Zero);
        IReadOnlyList<string> inventory = await probe.CaptureKeysAsync(Tenant, Intake, TestContext.Current.CancellationToken);
        string child = inventory.Single(key => key.Contains(":participant-resolution:", StringComparison.Ordinal));
        object value = (await store.GetAsync<object>(StoreName, child, TestContext.Current.CancellationToken)).Value!;
        foreach (string key in inventory)
        {
            await EraseAsync(store, key);
        }

        int beforeSweep = store.Reads;
        (await probe.AreAbsentAsync(Tenant, Intake, TestContext.Current.CancellationToken)).ShouldBeTrue();
        int sweepReads = store.Reads - beforeSweep;
        int boundaryAttempt = store.Reads + sweepReads + 1;
        store.OnReadAttempt = attempt =>
        {
            if (attempt == boundaryAttempt)
            {
                store.SaveAsync(StoreName, child, value).GetAwaiter().GetResult();
            }
        };

        (await probe.RemainsAbsentAsync(Tenant, Intake, TimeSpan.Zero, TestContext.Current.CancellationToken))
            .ShouldBeFalse();
        (await store.TryReadEtagAsync(StoreName, child, TestContext.Current.CancellationToken)).Present.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task IntakeReferencesRetainAssociationDiscoveryWhenSharedIndexesAreMissing(
        bool eraseTenantProjects,
        bool eraseProjectItems)
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, Intake);
        if (eraseTenantProjects)
        {
            await EraseAsync(store, $"{Tenant}:project-conversation:projects:index");
        }

        if (eraseProjectItems)
        {
            await EraseAsync(store, $"{Tenant}:project-conversation:project-001:index");
        }

        RecoveryIntakeReadModelProbe probe = new(store, store);
        (await probe.CaptureKeysAsync(Tenant, Intake, TestContext.Current.CancellationToken)).ShouldContain(
            ProjectConversationItemView.KeyFor(Tenant, "project-001", "email-" + Intake));
    }

    [Fact]
    public async Task InventoryIncludesTheSameItemIdentityInTwoProjectsAndTheirMaterializedChildren()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, "intake-002");
        await SeedAsync(store, Tenant, Intake);
        await SeedAsync(store, Tenant, Intake, "project-002");
        RecoveryIntakeReadModelProbe probe = new(store, store);
        IReadOnlyList<string> inventory = await probe.CaptureKeysAsync(Tenant, Intake, TestContext.Current.CancellationToken);

        inventory.Count.ShouldBe(12);
        inventory.Distinct(StringComparer.Ordinal).Count().ShouldBe(12);
        foreach (string project in new[] { "project-001", "project-002" })
        {
            inventory.ShouldContain(ProjectConversationItemView.KeyFor(Tenant, project, "email-" + Intake));
        }

        string[] preserved = store.Keys(StoreName).Except(inventory, StringComparer.Ordinal).ToArray();
        foreach (string key in inventory)
        {
            await EraseAsync(store, key);
        }

        store.Keys(StoreName).ShouldBe(preserved.Order(StringComparer.Ordinal));
        (await store.TryReadEtagAsync(StoreName,
            ProjectConversationItemView.KeyFor(Tenant, "project-001", "email-intake-002"),
            TestContext.Current.CancellationToken)).Present.ShouldBeTrue();
    }

    [Fact]
    public async Task ReassignedParticipantIdentityFailsInventoryWithoutErasingEitherIntake()
    {
        InMemoryRecoveryReadModelStore store = new();
        await SeedAsync(store, Tenant, Intake);
        string key = ParticipantResolutionView.KeyFor(Tenant, "resolution-" + Intake, "sender");
        ParticipantResolutionView original = (await store.GetAsync<ParticipantResolutionView>(
            StoreName, key, TestContext.Current.CancellationToken)).Value!;
        ReadModelProjectConversationProjectionStore projections = new(store);
        await projections.UpsertParticipantResolutionAsync(original with { IntakeId = "intake-002", SourceVersion = 2 },
            TestContext.Current.CancellationToken);
        IReadOnlyList<string> before = store.Keys(StoreName);
        RecoveryIntakeReadModelProbe probe = new(store, store);

        await Should.ThrowAsync<InvalidOperationException>(() => probe.CaptureKeysAsync(
            Tenant, Intake, TestContext.Current.CancellationToken));

        store.Erases.ShouldBe(0);
        store.Keys(StoreName).ShouldBe(before);
        (await store.GetAsync<ParticipantResolutionView>(StoreName, key, TestContext.Current.CancellationToken))
            .Value!.IntakeId.ShouldBe("intake-002");
    }

    /// <summary>Persists an intake and its real association, participant and attachment projections.</summary>
    internal static async Task SeedAsync(
        InMemoryRecoveryReadModelStore store,
        string tenant,
        string intake,
        string projectId = "project-001")
    {
        DateTimeOffset at = DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        ReadModelProjectConversationProjectionStore projections = new(store);
        ProjectConversationSourceEmailView source = new(tenant, intake, "mailbox-001", "message-001", null,
            "conversation-001", null, at, null, null, "UTC", "Microsoft 365 mailbox",
            AssociationCandidateView.MailboxSourceProvenance, "metadata_only", "collaboration_input",
            ProjectConversationSourceEmailView.CurrentSchemaVersion, 1, "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        ParticipantResolutionView participant = new(tenant, "resolution-" + intake, intake, "mailbox-001", "sender",
            "party-001", ParticipantResolutionStatus.Resolved, null, [],
            ProjectConversationParticipantDisplayKind.InternalParticipant, "Internal participant", "evidence:sender",
            "fingerprint", ParticipantResolutionView.CurrentSchemaVersion, ParticipantResolutionView.MailboxSourceProvenance,
            ParticipantResolutionView.CurrentDerivationKernelVersion, "metadata_only", "collaboration_input", 1,
            source.CorrelationId, at, at);
        ProjectConversationAttachmentReferenceView attachment = ProjectConversationAttachmentReferenceView.FromReference(
            tenant, intake, new MailboxAttachmentReference("attachment-001", "report.txt", "text/plain", 10),
            0, "metadata_only", "collaboration_input", 1, source.CorrelationId);
        ProjectConversationItemView association = new(tenant, projectId, "Project One", "email-" + intake, intake,
            ProjectConversationItemKind.EmailDerived, ProjectConversationActorKind.Mailbox, "Mailbox event", at,
            LifecycleState.Associated, AssociationThresholdBand.Auto, 0.9, "association-" + intake, "mailbox-001",
            null, null, "conversation-001", null, null, null, null, null, null,
            AssociationCandidateView.MailboxSourceProvenance, "metadata_only", "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion, 1, source.CorrelationId);
        await projections.UpsertSourceEmailAsync(source, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await projections.UpsertParticipantResolutionAsync(participant, TestContext.Current.CancellationToken).ConfigureAwait(false);
        await projections.UpsertAttachmentReferencesAsync(new(tenant, intake, [attachment], 1, source.CorrelationId),
            TestContext.Current.CancellationToken).ConfigureAwait(false);
        await projections.UpsertAsync(association, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static async Task EraseAsync(InMemoryRecoveryReadModelStore store, string key)
    {
        (bool present, string etag) = await store.TryReadEtagAsync(StoreName, key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        present.ShouldBeTrue();
        (await store.TryEraseAsync(StoreName, key, etag, TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBeTrue();
    }
}
