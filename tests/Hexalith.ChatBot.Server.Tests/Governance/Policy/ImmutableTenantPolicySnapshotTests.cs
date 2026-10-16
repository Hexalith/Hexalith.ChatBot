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

        snapshot.Values.Values.Count.ShouldBe(5);
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
        TenantPolicyChangeSet values = new([.. TenantPolicySchema.DefaultM0Values,
            new(TenantPolicyKnobIds.ApprovalRouting, StringValue: "synthetic-routing"),
            new(TenantPolicyKnobIds.AdminPermissionScopes, AdminScopesValue: scopes),
            new(TenantPolicyKnobIds.AllowlistVersionPin, StringValue: "synthetic-v1"),
            new(TenantPolicyKnobIds.ClassifierExplanationLayerEnabled, BoolValue: false),
            new(TenantPolicyKnobIds.InboundAuthenticityStrictness, StringValue: "strict"),
            new(TenantPolicyKnobIds.ApprovalPriorityWeights, ApprovalPriorityWeightsValue: new(1, 1, 1)),
            new(TenantPolicyKnobIds.NotificationThrottleCeilings, NotificationThrottleCeilingsValue: new(1, 1)),
            new(TenantPolicyKnobIds.ReviewerBacklogThreshold, ReviewerBacklogThresholdValue: new(1))]);
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

    [Fact]
    public void SyntheticReplacementAndRollbackMustPreserveHistoricalDecisionReferences()
    {
        TenantPolicySnapshotHistory history = new();
        history.TryAppend(Snapshot(), out history, out _).ShouldBeTrue();
        ImmutableTenantPolicySnapshot original = Resolve(history, "synthetic-first", 1)!;
        AiActionPolicyDecision originalDecision = AiActionPolicyDecision.Allowed(new(original.TenantId, original.PolicyVersion, original.SnapshotId,
            true, "read-only", ["summarize-visible-context"], true, true));
        history.TryAppend(Snapshot(id: "synthetic-next", version: 2, supersedes: "synthetic-first", values: Values(0.95)), out history, out _).ShouldBeTrue();
        history.TryAppend(Snapshot(id: "synthetic-rollback", version: 3, supersedes: "synthetic-next", rollback: "synthetic-first"), out history, out _).ShouldBeTrue();

        Resolve(history, "synthetic-first", 1).ShouldBeSameAs(original);
        originalDecision.TenantId.ShouldBe("synthetic-alpha");
        originalDecision.PolicyVersion.ShouldBe(1);
        originalDecision.PolicySnapshotId.ShouldBe("synthetic-first");
        original.Values.Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh).NumberValue.ShouldBe(0.90);
        history.Snapshots.Count.ShouldBe(3);
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

    private static ImmutableTenantPolicySnapshot Snapshot(string tenant = "synthetic-alpha", string id = "synthetic-first", long version = 1,
        string? supersedes = null, string? rollback = null, TenantPolicyChangeSet? values = null, DateTimeOffset? effective = null)
        => new(tenant, id, version, TenantPolicySchemaVersions.M0, effective ?? SyntheticNow.AddMinutes(-10).AddSeconds(version), "synthetic-actor",
            "synthetic-provenance", "synthetic-redaction", "synthetic-retention", "synthetic-hold", "synthetic-export-delete", "synthetic-backup", "synthetic-encryption",
            values ?? new(TenantPolicySchema.DefaultM0Values), supersedes, rollback);
}
