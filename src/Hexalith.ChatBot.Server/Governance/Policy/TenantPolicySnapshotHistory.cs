namespace Hexalith.ChatBot.Server.Governance.Policy;

/// <summary>A pure synthetic append-only fold, with no runtime registration or persistence path.</summary>
internal sealed class TenantPolicySnapshotHistory
{
    /// <summary>Creates an empty synthetic tenant history.</summary>
    public TenantPolicySnapshotHistory() : this([]) { }

    private TenantPolicySnapshotHistory(ImmutableTenantPolicySnapshot[] snapshots)
        => Snapshots = Array.AsReadOnly(snapshots);

    /// <summary>Gets immutable historical versions in append order.</summary>
    public IReadOnlyList<ImmutableTenantPolicySnapshot> Snapshots { get; }

    /// <summary>Appends a prospective version, replays exact reuse, or retains this fold on conflict.</summary>
    public bool TryAppend(ImmutableTenantPolicySnapshot snapshot, out TenantPolicySnapshotHistory history, out bool replay)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        history = this;
        replay = false;
        ImmutableTenantPolicySnapshot? reused = Snapshots.FirstOrDefault(existing =>
            existing.SnapshotId == snapshot.SnapshotId || existing.PolicyVersion == snapshot.PolicyVersion);
        if (reused is not null)
        {
            replay = reused.HasSameContents(snapshot);
            return replay;
        }

        ImmutableTenantPolicySnapshot? previous = Snapshots.LastOrDefault();
        if (previous is null ? snapshot.SupersedesSnapshotId is not null || snapshot.RollbackSnapshotId is not null :
            snapshot.TenantId != previous.TenantId || snapshot.PolicyVersion <= previous.PolicyVersion ||
            snapshot.EffectiveAtUtc <= previous.EffectiveAtUtc || snapshot.SupersedesSnapshotId != previous.SnapshotId)
        {
            return false;
        }

        if (snapshot.RollbackSnapshotId is not null && !Snapshots.Any(existing => existing.SnapshotId == snapshot.RollbackSnapshotId &&
            existing.SchemaVersion == snapshot.SchemaVersion &&
            existing.HasSameValues(snapshot)))
        {
            return false;
        }

        history = new TenantPolicySnapshotHistory([.. Snapshots, snapshot]);
        return true;
    }
}
