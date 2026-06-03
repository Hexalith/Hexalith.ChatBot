namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The pure, deterministic projection-rebuild equivalence function (Story 9.12, AC2/AC4). Given the pre-rebuild and
/// rebuilt structural snapshots (ordered sets of <see cref="ProjectionResourceDigest"/>) plus the two stamped projection
/// schema versions, it returns a <see cref="ProjectionRebuildVerdicts"/> token. No clock, no IO — re-running over the
/// same two snapshots yields the same verdict and the same first-diverging locator (mirroring
/// <see cref="ContinuityDrillEvaluator"/> and the pure verifiers behind the 9.4/9.5 isolation probes).
/// <para>
/// The evaluator is binary <c>equivalent</c>/<c>divergent</c> over <b>available</b> snapshots; the
/// <c>unmeasurable</c> verdict for a validation that could not complete is produced by the coordinator via
/// <see cref="ProjectionRebuildReport.Unmeasurable"/> (fail-safe), never here.
/// </para>
/// <para>
/// A schema-version mismatch is divergence (the event-upcasting / schema-churn failure mode of architecture invariant
/// #11 — derived-state versioning &amp; deterministic replay): a rebuild that stamps a different schema version makes
/// evidence snapshots / approval records non-reproducible.
/// </para>
/// </summary>
internal static class ProjectionRebuildEquivalenceEvaluator
{
    /// <summary>The deviation token recorded when the rebuilt projection diverges from the pre-rebuild projection.</summary>
    public const string DivergedDeviation = "projection_diverged";

    /// <summary>The deviation token recorded when the measured rebuild duration exceeds <see cref="RecoveryTargets.MaxRto"/>.</summary>
    public const string DurationExceededDeviation = "rebuild_duration_exceeded";

    /// <summary>
    /// Returns <see cref="ProjectionRebuildVerdicts.Equivalent"/> <b>iff</b> (a) the two projection schema versions are
    /// ordinally equal, (b) both snapshots cover the same resource-key set, and (c) every per-resource
    /// <see cref="ProjectionResourceDigest.StructuralStateToken"/> matches; otherwise
    /// <see cref="ProjectionRebuildVerdicts.Divergent"/>. Pure — no clock, no IO. Comparison is order-independent on the
    /// resource-key set.
    /// </summary>
    /// <param name="preRebuild">The pre-rebuild structural snapshot.</param>
    /// <param name="rebuilt">The rebuilt structural snapshot.</param>
    /// <param name="preRebuildSchemaVersion">The projection schema version stamped on the pre-rebuild snapshot.</param>
    /// <param name="rebuiltSchemaVersion">The projection schema version stamped on the rebuilt snapshot.</param>
    /// <returns>An <see cref="ProjectionRebuildVerdicts"/> token (<c>equivalent</c> or <c>divergent</c>).</returns>
    public static string Evaluate(
        IReadOnlyList<ProjectionResourceDigest> preRebuild,
        IReadOnlyList<ProjectionResourceDigest> rebuilt,
        string preRebuildSchemaVersion,
        string rebuiltSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(preRebuild);
        ArgumentNullException.ThrowIfNull(rebuilt);

        if (!string.Equals(preRebuildSchemaVersion, rebuiltSchemaVersion, StringComparison.Ordinal))
        {
            return ProjectionRebuildVerdicts.Divergent;
        }

        Dictionary<string, string> rebuiltByResource = ToTokenMap(rebuilt);
        if (rebuiltByResource.Count != preRebuild.Count)
        {
            // A different key count means a missing or extra resource — divergent regardless of token matches.
            return ProjectionRebuildVerdicts.Divergent;
        }

        foreach (ProjectionResourceDigest digest in preRebuild)
        {
            if (!rebuiltByResource.TryGetValue(digest.ResourceId, out string? rebuiltToken)
                || !string.Equals(rebuiltToken, digest.StructuralStateToken, StringComparison.Ordinal))
            {
                return ProjectionRebuildVerdicts.Divergent;
            }
        }

        return ProjectionRebuildVerdicts.Equivalent;
    }

    /// <summary>
    /// Returns a safe bounded locator (<c>resource:{safeId}</c> or <c>resource:unresolved</c>) for the first resource —
    /// <b>in the pre-rebuild snapshot's stable order</b> — that is missing from, extra in, or mismatched against the
    /// rebuilt snapshot. Deterministic across runs. Returns <see langword="null"/> when the snapshots are equivalent on
    /// the resource key/token dimension.
    /// </summary>
    /// <param name="preRebuild">The pre-rebuild structural snapshot.</param>
    /// <param name="rebuilt">The rebuilt structural snapshot.</param>
    /// <returns>The safe bounded first-diverging locator, or <see langword="null"/> when equivalent.</returns>
    public static string? FirstDivergingResourceLocator(
        IReadOnlyList<ProjectionResourceDigest> preRebuild,
        IReadOnlyList<ProjectionResourceDigest> rebuilt)
    {
        ArgumentNullException.ThrowIfNull(preRebuild);
        ArgumentNullException.ThrowIfNull(rebuilt);

        Dictionary<string, string> rebuiltByResource = ToTokenMap(rebuilt);

        // Walk the pre-rebuild snapshot in its stable order: the first missing-or-mismatched resource is the locator.
        foreach (ProjectionResourceDigest digest in preRebuild)
        {
            if (!rebuiltByResource.TryGetValue(digest.ResourceId, out string? rebuiltToken)
                || !string.Equals(rebuiltToken, digest.StructuralStateToken, StringComparison.Ordinal))
            {
                return Locator(digest.ResourceId);
            }
        }

        // No pre-rebuild resource diverged; the first EXTRA resource in the rebuilt snapshot (stable order) is the locator.
        HashSet<string> preRebuildKeys = new(StringComparer.Ordinal);
        foreach (ProjectionResourceDigest digest in preRebuild)
        {
            preRebuildKeys.Add(digest.ResourceId);
        }

        foreach (ProjectionResourceDigest digest in rebuilt)
        {
            if (!preRebuildKeys.Contains(digest.ResourceId))
            {
                return Locator(digest.ResourceId);
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the bounded deviation tokens for a verdict + duration outcome, in a stable order
    /// (<see cref="DivergedDeviation"/>, <see cref="DurationExceededDeviation"/>). Empty when the rebuild is
    /// <see cref="ProjectionRebuildVerdicts.Equivalent"/> and within target.
    /// </summary>
    /// <param name="verdict">The equivalence verdict.</param>
    /// <param name="durationWithinTarget">Whether the measured rebuild duration is within <see cref="RecoveryTargets.MaxRto"/>.</param>
    /// <returns>The bounded deviation tokens.</returns>
    public static IReadOnlyList<string> Deviations(string verdict, bool durationWithinTarget)
    {
        List<string> deviations = [];
        if (string.Equals(verdict, ProjectionRebuildVerdicts.Divergent, StringComparison.Ordinal))
        {
            deviations.Add(DivergedDeviation);
        }

        if (!durationWithinTarget)
        {
            deviations.Add(DurationExceededDeviation);
        }

        return deviations;
    }

    private static string Locator(string resourceId)
        => AuditMetadata.SafeOptionalToken($"resource:{resourceId}") ?? "resource:unresolved";

    private static Dictionary<string, string> ToTokenMap(IReadOnlyList<ProjectionResourceDigest> snapshot)
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (ProjectionResourceDigest digest in snapshot)
        {
            map[digest.ResourceId] = digest.StructuralStateToken;
        }

        return map;
    }
}
