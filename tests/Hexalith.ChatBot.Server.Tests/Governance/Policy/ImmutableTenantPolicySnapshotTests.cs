using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Policy;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.Policy;

/// <summary>Synthetic in-memory invariants only; these tests are not native isolation or A6 acceptance.</summary>
public sealed class ImmutableTenantPolicySnapshotTests
{
    private static readonly DateTimeOffset SyntheticNow = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SyntheticSnapshotMustDefensivelyCopyEveryMutableCollection()
    {
        Dictionary<AiActionRiskActionClass, bool> map = TenantPolicySchema.RequiredAiActionClasses.ToDictionary(static key => key, static _ => false);
        List<string> routes = ["synthetic-route"];
        List<TenantPolicyValue> values = [.. TenantPolicySchema.DefaultM0Values.Where(static value =>
            value.KnobId is not (TenantPolicyKnobIds.AiActionLowRiskAllowed or TenantPolicyKnobIds.MailboxRoutingRules)),
            new(TenantPolicyKnobIds.AiActionLowRiskAllowed, AiActionLowRiskAllowed: map),
            new(TenantPolicyKnobIds.MailboxRoutingRules, StringListValue: routes)];
        ImmutableTenantPolicySnapshot snapshot = Snapshot(values: new(values));
        map[AiActionRiskActionClass.ModifiesState] = true;
        routes[0] = "changed";
        values.Clear();

        snapshot.Values.Values.Count.ShouldBe(TenantPolicySchema.DefaultM0Values.Count);
        IReadOnlyDictionary<AiActionRiskActionClass, bool> copiedMap = snapshot.Values.Values.Single(static value =>
            value.KnobId == TenantPolicyKnobIds.AiActionLowRiskAllowed).AiActionLowRiskAllowed!;
        copiedMap[AiActionRiskActionClass.ModifiesState].ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => ((IDictionary<AiActionRiskActionClass, bool>)copiedMap)[AiActionRiskActionClass.ModifiesState] = true);
        IReadOnlyList<string> copiedRoutes = snapshot.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.MailboxRoutingRules).StringListValue!;
        copiedRoutes.ShouldBe(["synthetic-route"]);
        Should.Throw<NotSupportedException>(() => ((IList<string>)copiedRoutes)[0] = "changed");
        Should.Throw<NotSupportedException>(() => ((IList<TenantPolicyValue>)snapshot.Values.Values).Clear());
        snapshot.EvidenceKind.ShouldBe("synthetic-unaccepted");
    }

    [Fact]
    public void SyntheticM1AdminScopesMustBeCopiedWithoutExposingMutableHistory()
    {
        List<AdminScope> scopes = [AdminScope.Policy];
        TenantPolicyChangeSet values = M1Values(scopes);
        ImmutableTenantPolicySnapshot snapshot = new("synthetic-alpha", "synthetic-m1", 1, TenantPolicySchemaVersions.M1Preview,
            SyntheticNow, "synthetic-actor", "synthetic-provenance", "synthetic-redaction", "synthetic-retention", "synthetic-hold",
            "synthetic-export-delete", "synthetic-backup", "synthetic-encryption", values);
        scopes.Clear();
        IReadOnlyList<AdminScope> retained = snapshot.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AdminPermissionScopes).AdminScopesValue!;
        retained.ShouldBe([AdminScope.Policy]);
        Should.Throw<NotSupportedException>(() => ((IList<AdminScope>)retained).Clear());
    }

    [Fact]
    public void SyntheticHistoryMustReplayEquivalentContentsAndRetainOriginalOnIdentityOrVersionConflict()
    {
        TenantPolicySnapshotHistory empty = new();
        empty.TryAppend(Snapshot(), out TenantPolicySnapshotHistory first, out bool replay).ShouldBeTrue();
        replay.ShouldBeFalse();
        empty.Snapshots.ShouldBeEmpty();
        first.TryAppend(Snapshot(values: new(TenantPolicySchema.DefaultM0Values.Reverse().ToArray())), out TenantPolicySnapshotHistory same, out replay).ShouldBeTrue();
        replay.ShouldBeTrue();
        same.ShouldBeSameAs(first);
        foreach (ImmutableTenantPolicySnapshot conflict in new[] { Snapshot(values: Values(0.95)), Snapshot(id: "synthetic-other"), Snapshot(tenant: "synthetic-foreign"),
            Snapshot(id: "synthetic-first", version: 2, supersedes: "synthetic-first") })
        {
            first.TryAppend(conflict, out TenantPolicySnapshotHistory retained, out replay).ShouldBeFalse();
            retained.ShouldBeSameAs(first);
            replay.ShouldBeFalse();
        }
        first.Snapshots.Single().Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh).NumberValue.ShouldBe(0.90);
    }

    /// <summary>Rejects immutable metadata changes under an already retained identity and version.</summary>
    [Theory]
    [InlineData("actor")]
    [InlineData("effective-time")]
    [InlineData("provenance")]
    [InlineData("redaction")]
    [InlineData("retention")]
    [InlineData("hold")]
    [InlineData("export-delete")]
    [InlineData("backup")]
    [InlineData("encryption")]
    [InlineData("supersedes")]
    [InlineData("rollback")]
    public void SyntheticMetadataConflictsMustRejectAndRetainOriginalHistory(string changedField)
    {
        ImmutableTenantPolicySnapshot original = Snapshot();
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(original, out history, out _).ShouldBeTrue();
        ImmutableTenantPolicySnapshot conflict = new(original.TenantId, original.SnapshotId, original.PolicyVersion, original.SchemaVersion,
            changedField == "effective-time" ? original.EffectiveAtUtc.AddTicks(1) : original.EffectiveAtUtc,
            changedField == "actor" ? "synthetic-other" : original.ActorId,
            changedField == "provenance" ? "synthetic-other" : original.ProvenanceReference,
            changedField == "redaction" ? "synthetic-other" : original.RedactionReference,
            changedField == "retention" ? "synthetic-other" : original.RetentionReference,
            changedField == "hold" ? "synthetic-other" : original.HoldReference,
            changedField == "export-delete" ? "synthetic-other" : original.ExportDeleteReference,
            changedField == "backup" ? "synthetic-other" : original.BackupReference,
            changedField == "encryption" ? "synthetic-other" : original.EncryptionReference, original.Values,
            changedField == "supersedes" ? "synthetic-other" : original.SupersedesSnapshotId,
            changedField == "rollback" ? "synthetic-other" : original.RollbackSnapshotId);

        history.TryAppend(conflict, out TenantPolicySnapshotHistory retained, out bool replay).ShouldBeFalse();
        replay.ShouldBeFalse();
        retained.ShouldBeSameAs(history);
        retained.Snapshots.ShouldHaveSingleItem().ShouldBeSameAs(original);
        original.HasSameContents(Snapshot()).ShouldBeTrue();
    }

    /// <summary>Rejects a schema-version change under an already retained identity and version.</summary>
    [Fact]
    public void SyntheticSchemaVersionConflictMustRejectAndRetainOriginalHistory()
    {
        ImmutableTenantPolicySnapshot original = Snapshot(schema: TenantPolicySchemaVersions.M1Preview, values: M1Values([AdminScope.Policy], null));
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(original, out history, out _).ShouldBeTrue();
        ImmutableTenantPolicySnapshot conflict = Snapshot(schema: TenantPolicySchemaVersions.M0);

        history.TryAppend(conflict, out TenantPolicySnapshotHistory retained, out bool replay).ShouldBeFalse();
        replay.ShouldBeFalse();
        retained.ShouldBeSameAs(history);
        retained.Snapshots.ShouldHaveSingleItem().ShouldBeSameAs(original);
    }

    /// <summary>Rejects nested value differences on identity reuse and rollback without changing retained snapshots.</summary>
    [Theory]
    [InlineData(TenantPolicyKnobIds.AiActionLowRiskAllowed, false)]
    [InlineData(TenantPolicyKnobIds.MailboxRoutingRules, false)]
    [InlineData(TenantPolicyKnobIds.AdminPermissionScopes, false)]
    [InlineData(TenantPolicyKnobIds.AiActionLowRiskAllowed, true)]
    [InlineData(TenantPolicyKnobIds.MailboxRoutingRules, true)]
    [InlineData(TenantPolicyKnobIds.AdminPermissionScopes, true)]
    public void SyntheticNestedValueConflictsMustRejectIdentityReuseAndRollback(string changedKnob, bool rollback)
    {
        TenantPolicyChangeSet originalValues = new(M1Values().Values.Select(static value => value.KnobId == TenantPolicyKnobIds.MailboxRoutingRules
            ? value with { StringListValue = ["synthetic-original-route"] } : value).ToArray());
        ImmutableTenantPolicySnapshot original = Snapshot(schema: TenantPolicySchemaVersions.M1Preview, values: originalValues);
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(original, out history, out _).ShouldBeTrue();
        TenantPolicyChangeSet changedValues = new(original.Values.Values.Select(value => value.KnobId != changedKnob ? value : changedKnob switch
        {
            TenantPolicyKnobIds.AiActionLowRiskAllowed => value with
            {
                AiActionLowRiskAllowed = value.AiActionLowRiskAllowed!.ToDictionary(static pair => pair.Key,
                    static pair => pair.Key == AiActionRiskActionClass.ModifiesState || pair.Value),
            },
            TenantPolicyKnobIds.MailboxRoutingRules => value with { StringListValue = ["synthetic-different-route"] },
            TenantPolicyKnobIds.AdminPermissionScopes => value with { AdminScopesValue = [AdminScope.SeeOnly] },
            _ => throw new ArgumentOutOfRangeException(nameof(changedKnob)),
        }).ToArray());
        if (rollback)
        {
            history.TryAppend(Snapshot(id: "synthetic-next", version: 2, supersedes: original.SnapshotId,
                schema: original.SchemaVersion, values: changedValues), out history, out _).ShouldBeTrue();
        }
        ImmutableTenantPolicySnapshot[] beforeConflict = history.Snapshots.ToArray();
        ImmutableTenantPolicySnapshot conflict = Snapshot(id: rollback ? "synthetic-rollback" : original.SnapshotId,
            version: rollback ? 3 : original.PolicyVersion, supersedes: rollback ? "synthetic-next" : null,
            rollback: rollback ? original.SnapshotId : null, schema: original.SchemaVersion, values: changedValues);

        history.TryAppend(conflict, out TenantPolicySnapshotHistory retained, out bool replay).ShouldBeFalse();
        replay.ShouldBeFalse();
        retained.ShouldBeSameAs(history);
        retained.Snapshots.ShouldBe(beforeConflict);
        retained.Snapshots[0].ShouldBeSameAs(original);
        original.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AiActionLowRiskAllowed)
            .AiActionLowRiskAllowed!.Values.ShouldAllBe(static allowed => !allowed);
        original.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.MailboxRoutingRules)
            .StringListValue.ShouldBe(["synthetic-original-route"]);
        original.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AdminPermissionScopes)
            .AdminScopesValue.ShouldBe([AdminScope.Policy]);
    }

    /// <summary>Canonicalizes separately allocated nested maps and scope collections for equivalent replay.</summary>
    [Fact]
    public void SyntheticEquivalentReplayMustCanonicalizeNestedCollectionOrder()
    {
        Dictionary<AiActionRiskActionClass, bool> firstMap = TenantPolicySchema.RequiredAiActionClasses.ToDictionary(static key => key, static _ => false);
        Dictionary<AiActionRiskActionClass, bool> secondMap = TenantPolicySchema.RequiredAiActionClasses.Reverse().ToDictionary(static key => key, static _ => false);
        TenantPolicySnapshotHistory history = new();
        ImmutableTenantPolicySnapshot original = Snapshot(schema: TenantPolicySchemaVersions.M1Preview,
            values: M1Values([AdminScope.Policy, AdminScope.SeeOnly], firstMap));
        history.TryAppend(original, out history, out _).ShouldBeTrue();
        ImmutableTenantPolicySnapshot equivalent = Snapshot(schema: TenantPolicySchemaVersions.M1Preview,
            values: M1Values([AdminScope.SeeOnly, AdminScope.Policy], secondMap));

        history.TryAppend(equivalent, out TenantPolicySnapshotHistory replayed, out bool replay).ShouldBeTrue();
        replay.ShouldBeTrue();
        replayed.ShouldBeSameAs(history);
        replayed.Snapshots.ShouldHaveSingleItem().ShouldBeSameAs(original);
    }

    /// <summary>Rejects an unused lower version and an equal effective time without replacing history.</summary>
    [Theory]
    [InlineData("lower-version")]
    [InlineData("equal-effective-time")]
    public void SyntheticAppendMustRequireStrictlyProspectiveVersionAndEffectiveTime(string boundary)
    {
        ImmutableTenantPolicySnapshot original = Snapshot(version: 3);
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(original, out history, out _).ShouldBeTrue();
        ImmutableTenantPolicySnapshot candidate = Snapshot(id: "synthetic-next", version: boundary == "lower-version" ? 2 : 4,
            supersedes: original.SnapshotId, effective: boundary == "lower-version" ? original.EffectiveAtUtc.AddTicks(1) : original.EffectiveAtUtc);

        history.TryAppend(candidate, out TenantPolicySnapshotHistory retained, out bool replay).ShouldBeFalse();
        replay.ShouldBeFalse();
        retained.ShouldBeSameAs(history);
        retained.Snapshots.ShouldHaveSingleItem().ShouldBeSameAs(original);
    }

    /// <summary>Keeps M0 and M1 declared knob sets separate and complete, and rejects unknown schema versions.</summary>
    [Fact]
    public void SyntheticSchemaMustRejectM1KnobsInM0AndIncompleteM1Values()
    {
        Should.Throw<ArgumentException>(() => Snapshot(schema: "synthetic-unknown-schema"));
        TenantPolicyChangeSet completeM1 = M1Values();
        Snapshot(schema: TenantPolicySchemaVersions.M1Preview, values: completeM1).SchemaVersion.ShouldBe(TenantPolicySchemaVersions.M1Preview);
        Should.Throw<ArgumentException>(() => Snapshot(values: new([.. TenantPolicySchema.DefaultM0Values,
            new(TenantPolicyKnobIds.ApprovalRouting, StringValue: "synthetic-routing")])));
        Should.Throw<ArgumentException>(() => Snapshot(schema: TenantPolicySchemaVersions.M1Preview,
            values: new(completeM1.Values.Where(static value => value.KnobId != TenantPolicyKnobIds.ApprovalRouting).ToArray())));
    }

    /// <summary>Pins UTC evidence and current-time requirements and the exact five-minute freshness limit.</summary>
    [Theory]
    [InlineData("evidence-offset", false)]
    [InlineData("current-offset", false)]
    [InlineData("five-minutes", true)]
    [InlineData("five-minutes-plus-tick", false)]
    public void SyntheticResolutionMustEnforceUtcAndExactFreshnessBoundaries(string boundary, bool allowed)
    {
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(Snapshot(), out history, out _).ShouldBeTrue();
        DateTimeOffset evidence = SyntheticNow.AddMinutes(-5);
        DateTimeOffset current = SyntheticNow;
        if (boundary == "evidence-offset") { evidence = evidence.ToOffset(TimeSpan.FromHours(1)); }
        if (boundary == "current-offset") { current = current.ToOffset(TimeSpan.FromHours(1)); }
        if (boundary == "five-minutes-plus-tick") { evidence = evidence.AddTicks(-1); }

        ImmutableTenantPolicySnapshot? resolved = TenantPolicySnapshotResolver.Resolve([history], Context("synthetic-alpha"),
            "synthetic-first", 1, true, evidence, current);
        if (allowed) { resolved.ShouldBeSameAs(history.Snapshots.Single()); }
        else { resolved.ShouldBeNull(); }
    }

    [Fact]
    public void SyntheticReplacementAndRollbackMustPreserveHistoricalDecisionReferences()
    {
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(Snapshot(), out history, out _).ShouldBeTrue();
        ImmutableTenantPolicySnapshot original = Resolve(history, "synthetic-first", 1)!;
        AiActionPolicyDecision originalDecision = AiActionPolicyDecision.Allowed(new(original.TenantId, original.PolicyVersion, original.SnapshotId,
            true, "read-only", ["summarize-visible-context"], true, true));
        foreach (ImmutableTenantPolicySnapshot successor in new[]
        {
            Snapshot(id: "synthetic-next", version: 2, supersedes: "synthetic-first", values: Values(0.95)),
            Snapshot(id: "synthetic-rollback", version: 3, supersedes: "synthetic-next", rollback: "synthetic-first"),
        })
        {
            history.TryAppend(successor, out history, out _).ShouldBeTrue();
            ImmutableTenantPolicySnapshot[] beforeReplay = history.Snapshots.ToArray();
            history.TryAppend(Snapshot(), out TenantPolicySnapshotHistory replayed, out bool replay).ShouldBeTrue();
            replay.ShouldBeTrue();
            replayed.ShouldBeSameAs(history);
            replayed.Snapshots.ShouldBe(beforeReplay);
            TenantPolicySnapshotResolver.Resolve([history], Context(originalDecision.TenantId!), originalDecision.PolicySnapshotId,
                originalDecision.PolicyVersion!.Value, true, SyntheticNow, SyntheticNow).ShouldBeSameAs(original);
        }

        Resolve(history, "synthetic-first", 1).ShouldBeSameAs(original);
        original.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh).NumberValue.ShouldBe(0.90);
        history.Snapshots.Count.ShouldBe(3);
        history.Snapshots[1].Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh).NumberValue.ShouldBe(0.95);
        history.Snapshots[2].Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh).NumberValue.ShouldBe(0.90);
        history.Snapshots[2].RollbackSnapshotId.ShouldBe("synthetic-first");
        history.TryAppend(Snapshot(id: "synthetic-invalid", version: 4, supersedes: "synthetic-rollback", rollback: "synthetic-first", values: Values(0.99)), out _, out _).ShouldBeFalse();
        history.TryAppend(Snapshot(id: "synthetic-backdated", version: 4, supersedes: "synthetic-rollback", effective: SyntheticNow.AddMinutes(-30)), out _, out _).ShouldBeFalse();
        history.TryAppend(Snapshot(id: "synthetic-foreign", tenant: "synthetic-foreign", version: 4, supersedes: "synthetic-rollback"), out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void SyntheticResolutionMustBeExactAndTenantBoundWithoutAnyDefault()
    {
        TenantPolicySnapshotHistory alpha = new();
        TenantPolicySnapshotHistory beta = new();
        alpha.TryAppend(Snapshot(), out alpha, out _).ShouldBeTrue();
        beta.TryAppend(Snapshot(tenant: "synthetic-beta"), out beta, out _).ShouldBeTrue();
        TenantPolicySnapshotResolver.Resolve([alpha, beta], Context("synthetic-alpha"), "synthetic-first", 1, true, SyntheticNow, SyntheticNow).ShouldBeSameAs(alpha.Snapshots.Single());
        TenantPolicySnapshotResolver.Resolve([alpha, beta], Context("synthetic-beta"), "synthetic-first", 1, true, SyntheticNow, SyntheticNow).ShouldBeSameAs(beta.Snapshots.Single());
        foreach (string? id in new[] { null, "", "unknown", "malformed/id" })
        {
            Resolve(alpha, id, 1).ShouldBeNull();
        }
        Resolve(alpha, "synthetic-first", 2).ShouldBeNull();
        TenantPolicySnapshotResolver.Resolve([alpha], Context("synthetic-beta"), "synthetic-first", 1, true, SyntheticNow, SyntheticNow).ShouldBeNull();
        TenantPolicySnapshotResolver.Resolve([alpha], Context("synthetic-alpha"), "synthetic-first", 1, false, SyntheticNow, SyntheticNow).ShouldBeNull();
        TenantPolicySnapshotResolver.Resolve([alpha], Context("synthetic-alpha"), "synthetic-first", 1, true, SyntheticNow.AddMinutes(-5).AddTicks(-1), SyntheticNow).ShouldBeNull();
        TenantPolicySnapshotResolver.Resolve([alpha], Context("synthetic-alpha"), "synthetic-first", 1, true, SyntheticNow.AddTicks(1), SyntheticNow).ShouldBeNull();
        TenantPolicySnapshotResolver.Resolve([alpha, alpha], Context("synthetic-alpha"), "synthetic-first", 1, true, SyntheticNow, SyntheticNow).ShouldBeNull();
    }

    [Fact]
    public void SyntheticResolutionMustDenyFutureEffectiveAndUnauthenticatedCallers()
    {
        TenantPolicySnapshotHistory future = new();
        future.TryAppend(Snapshot(effective: SyntheticNow.AddMinutes(1)), out future, out _).ShouldBeTrue();
        Resolve(future, "synthetic-first", 1).ShouldBeNull();

        TenantPolicySnapshotHistory current = new();
        current.TryAppend(Snapshot(), out current, out _).ShouldBeTrue();
        ChatBotRequestContext unauthenticated = new("synthetic-actor", "synthetic-alpha", "human", null, ChatBotSurfaceOrigin.Api,
            new ClaimsPrincipal(new ClaimsIdentity([new("sub", "synthetic-actor")])));
        TenantPolicySnapshotResolver.Resolve([current], unauthenticated, "synthetic-first", 1, true, SyntheticNow, SyntheticNow).ShouldBeNull();
    }

    [Fact]
    public void SyntheticHistoryMustRejectBrokenSupersessionAndRollbackLineage()
    {
        TenantPolicySnapshotHistory empty = new();
        empty.TryAppend(Snapshot(supersedes: "synthetic-ghost"), out _, out _).ShouldBeFalse();
        empty.TryAppend(Snapshot(rollback: "synthetic-ghost"), out _, out _).ShouldBeFalse();

        TenantPolicySnapshotHistory history = new();
        history.TryAppend(Snapshot(), out history, out _).ShouldBeTrue();
        history.TryAppend(Snapshot(id: "synthetic-next", version: 2, supersedes: "synthetic-ghost"), out _, out _).ShouldBeFalse();
        history.TryAppend(Snapshot(id: "synthetic-next", version: 2, supersedes: "synthetic-first", rollback: "synthetic-ghost"), out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void SyntheticMalformedPolicyMustRejectUnknownKnobsIncompleteValuesAndUnsafeThresholds()
    {
        Should.Throw<ArgumentException>(() => Snapshot(values: new([new("unknown", BoolValue: true)])));
        Should.Throw<ArgumentException>(() => Snapshot(values: new([new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.9)])));
        TenantPolicyValue[] reversedUnsafe = [new(TenantPolicyKnobIds.AssociationTLow, NumberValue: 0.95),
            .. TenantPolicySchema.DefaultM0Values.Where(static value => value.KnobId != TenantPolicyKnobIds.AssociationTLow)];
        Should.Throw<ArgumentException>(() => Snapshot(values: new(reversedUnsafe)));
        Should.Throw<ArgumentException>(() => Snapshot(effective: SyntheticNow.ToOffset(TimeSpan.FromHours(1))));
    }

    private static ImmutableTenantPolicySnapshot? Resolve(TenantPolicySnapshotHistory history, string? id, long version)
        => TenantPolicySnapshotResolver.Resolve([history], Context("synthetic-alpha"), id, version, true, SyntheticNow, SyntheticNow);

    private static ChatBotRequestContext Context(string tenant)
    {
        ChatBotRequestContextResolver.TryResolve(new ClaimsPrincipal(new ClaimsIdentity([new("sub", "synthetic-actor"), new("eventstore:tenant", tenant)], "synthetic")),
            ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _).ShouldBeTrue();
        return context!;
    }

    private static TenantPolicyChangeSet Values(double high)
        => new(TenantPolicySchema.DefaultM0Values.Select(value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh ? value with { NumberValue = high } : value).ToArray());

    private static TenantPolicyChangeSet M1Values(IReadOnlyList<AdminScope>? scopes = null, IReadOnlyDictionary<AiActionRiskActionClass, bool>? actionMap = null)
        => new([.. TenantPolicySchema.DefaultM0Values.Select(value => value.KnobId == TenantPolicyKnobIds.AiActionLowRiskAllowed && actionMap is not null
                ? value with { AiActionLowRiskAllowed = actionMap } : value),
            new(TenantPolicyKnobIds.ApprovalRouting, StringValue: "synthetic-routing"),
            new(TenantPolicyKnobIds.AdminPermissionScopes, AdminScopesValue: scopes ?? [AdminScope.Policy]),
            new(TenantPolicyKnobIds.AllowlistVersionPin, StringValue: "synthetic-v1"),
            new(TenantPolicyKnobIds.ClassifierExplanationLayerEnabled, BoolValue: false),
            new(TenantPolicyKnobIds.InboundAuthenticityStrictness, StringValue: "strict"),
            new(TenantPolicyKnobIds.ApprovalPriorityWeights, ApprovalPriorityWeightsValue: new(1, 1, 1)),
            new(TenantPolicyKnobIds.NotificationThrottleCeilings, NotificationThrottleCeilingsValue: new(1, 1)),
            new(TenantPolicyKnobIds.ReviewerBacklogThreshold, ReviewerBacklogThresholdValue: new(1))]);

    private static ImmutableTenantPolicySnapshot Snapshot(string tenant = "synthetic-alpha", string id = "synthetic-first", long version = 1,
        string? supersedes = null, string? rollback = null, TenantPolicyChangeSet? values = null, DateTimeOffset? effective = null, string schema = TenantPolicySchemaVersions.M0)
        => new(tenant, id, version, schema, effective ?? SyntheticNow.AddMinutes(-10).AddSeconds(version), "synthetic-actor",
            "synthetic-provenance", "synthetic-redaction", "synthetic-retention", "synthetic-hold", "synthetic-export-delete", "synthetic-backup", "synthetic-encryption",
            values ?? new(TenantPolicySchema.DefaultM0Values), supersedes, rollback);
}
