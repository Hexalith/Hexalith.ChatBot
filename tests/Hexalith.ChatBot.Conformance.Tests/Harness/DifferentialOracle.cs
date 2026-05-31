using System.Globalization;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// One comparable admission step lifted from an audit envelope. The legitimately per-surface / per-run fields
/// (<c>surfaceOrigin</c>, the minted <c>commandId</c>/<c>resourceId</c>, the idempotency key, timestamps) are
/// deliberately NOT captured here — they are excluded from the differential by construction.
/// </summary>
internal sealed record AdmissionStep(
    string Phase,
    string StateTransition,
    string Decision,
    string ReasonCode,
    string Outcome,
    string RedactionDecision);

/// <summary>
/// The include-set projection of the durable <c>GovernedOperationView</c>: the derived-record shape and source
/// version that must be byte-identical across surfaces. Timestamps (<c>recordedAt</c>/<c>lastUpdatedAt</c>) are
/// excluded.
/// </summary>
internal sealed record DurableViewFacts(
    string NoteId,
    string SchemaVersion,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion);

/// <summary>
/// A single surface arm's captured two-layer outcome: the admission event sequence (where the surface origin
/// legitimately appears) plus the durable state-store end-state (origin-free, surface-invariant by
/// construction). Every field is read from the audit-envelope sequence or the state store — never from a bare
/// HTTP 202 / CLI exit / MCP response code.
/// </summary>
internal sealed record ArmOutcome(
    string ArmName,
    string DeclaredOrigin,
    string? AuditedOrigin,
    IReadOnlyList<AdmissionStep> AdmissionSequence,
    string AcceptedLifecycleState,
    string DomainOutcomeIdentity,
    int DispatchCount,
    int CoarseIdempotencyRecordCount,
    DurableViewFacts? DurableView);

/// <summary>The result of comparing two arm outcomes under the equality projection.</summary>
/// <param name="AreEqual">Whether the two outcomes are equal under the include/exclude projection.</param>
/// <param name="DivergingField">The first diverging field name, or <see langword="null"/> when equal.</param>
/// <param name="LeftValue">The left value at the diverging field.</param>
/// <param name="RightValue">The right value at the diverging field.</param>
internal sealed record OracleVerdict(bool AreEqual, string? DivergingField, string? LeftValue, string? RightValue)
{
    public static OracleVerdict Equal { get; } = new(true, null, null, null);
}

/// <summary>
/// The differential equality oracle. <see cref="Project"/> flattens an arm outcome into an ordered list of
/// comparable (field, value) pairs honoring the explicit include set and dropping the explicit exclude set
/// (<c>surfaceOrigin</c>, minted ids, timestamps). <see cref="Compare"/> walks the two projections in lock-step
/// and names the first diverging field, so a silently no-op (vacuous) equality is provable false by the
/// non-vacuity meta-test.
/// </summary>
internal static class DifferentialOracle
{
    /// <summary>Builds the ordered comparable (field, value) projection for an outcome.</summary>
    /// <param name="outcome">The captured arm outcome.</param>
    /// <returns>The ordered comparable fields (exclude-set values are never added).</returns>
    public static IReadOnlyList<KeyValuePair<string, string>> Project(ArmOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        List<KeyValuePair<string, string>> fields = [];
        void Add(string name, string value) => fields.Add(new KeyValuePair<string, string>(name, value));
        string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

        Add("lifecycle", outcome.AcceptedLifecycleState);
        Add("domainOutcome", outcome.DomainOutcomeIdentity);
        Add("dispatchCount", Int(outcome.DispatchCount));
        Add("coarseIdempotencyRecordCount", Int(outcome.CoarseIdempotencyRecordCount));
        Add("admission.count", Int(outcome.AdmissionSequence.Count));
        for (int i = 0; i < outcome.AdmissionSequence.Count; i++)
        {
            AdmissionStep step = outcome.AdmissionSequence[i];
            Add($"admission[{i}].phase", step.Phase);
            Add($"admission[{i}].stateTransition", step.StateTransition);
            Add($"admission[{i}].decision", step.Decision);
            Add($"admission[{i}].reasonCode", step.ReasonCode);
            Add($"admission[{i}].outcome", step.Outcome);
            Add($"admission[{i}].redactionDecision", step.RedactionDecision);
        }

        Add("view.present", (outcome.DurableView is not null).ToString());
        if (outcome.DurableView is { } view)
        {
            Add("view.noteId", view.NoteId);
            Add("view.schemaVersion", view.SchemaVersion);
            Add("view.sourceProvenance", view.SourceProvenance);
            Add("view.derivationKernelVersion", view.DerivationKernelVersion);
            Add("view.redactionState", view.RedactionState);
            Add("view.retentionClass", view.RetentionClass);
            Add("view.sourceVersion", view.SourceVersion.ToString(CultureInfo.InvariantCulture));
        }

        return fields;
    }

    /// <summary>Compares two arm outcomes, returning the first diverging field (or equality).</summary>
    /// <param name="left">The reference arm outcome.</param>
    /// <param name="right">The candidate arm outcome.</param>
    /// <returns>The verdict, naming the first diverging field on inequality.</returns>
    public static OracleVerdict Compare(ArmOutcome left, ArmOutcome right)
    {
        IReadOnlyList<KeyValuePair<string, string>> leftFields = Project(left);
        IReadOnlyList<KeyValuePair<string, string>> rightFields = Project(right);

        int count = Math.Max(leftFields.Count, rightFields.Count);
        for (int i = 0; i < count; i++)
        {
            if (i >= leftFields.Count)
            {
                return new OracleVerdict(false, rightFields[i].Key, "<absent>", rightFields[i].Value);
            }

            if (i >= rightFields.Count)
            {
                return new OracleVerdict(false, leftFields[i].Key, leftFields[i].Value, "<absent>");
            }

            if (!string.Equals(leftFields[i].Key, rightFields[i].Key, StringComparison.Ordinal))
            {
                return new OracleVerdict(false, $"{leftFields[i].Key}|{rightFields[i].Key}", leftFields[i].Value, rightFields[i].Value);
            }

            if (!string.Equals(leftFields[i].Value, rightFields[i].Value, StringComparison.Ordinal))
            {
                return new OracleVerdict(false, leftFields[i].Key, leftFields[i].Value, rightFields[i].Value);
            }
        }

        return OracleVerdict.Equal;
    }
}
