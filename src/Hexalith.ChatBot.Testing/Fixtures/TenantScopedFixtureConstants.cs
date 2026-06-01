using System.Reflection;
using System.Runtime.Serialization;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Testing.Fixtures;

/// <summary>
/// Stable wire values for the Story 1.13 tenant-scoped evaluation fixture scaffold.
/// </summary>
public static class TenantScopedFixtureConstants
{
    /// <summary>The embedded manifest resource logical name used by test projects.</summary>
    public const string ResourceName = "story-1-13-tenant-scoped-evaluation-dataset.json";

    /// <summary>The tenant-partition role whose tenant must own at least one fixture case.</summary>
    public const string OwnTenantRole = "own";

    /// <summary>The tenant-partition role used only for negative/adversarial cross-tenant references.</summary>
    public const string ForeignTenantRole = "foreign";

    /// <summary>The expected source classification for synthetic scaffold data.</summary>
    public const string SyntheticSourceClassification = "synthetic";

    /// <summary>The expected source classification for redacted scaffold data.</summary>
    public const string RedactedSourceClassification = "redacted";

    /// <summary>The expected source classification for explicitly consented scaffold data.</summary>
    public const string ConsentedSourceClassification = "consented";

    /// <summary>Calibration partition name.</summary>
    public const string CalibrationPartition = "calibration";

    /// <summary>Held-out regression partition name.</summary>
    public const string HeldOutRegressionPartition = "held-out-regression";

    /// <summary>Adversarial partition name.</summary>
    public const string AdversarialPartition = "adversarial";

    /// <summary>Required A9a labels.</summary>
    public static IReadOnlyList<string> RequiredLabels { get; } =
    [
        "deterministic-match",
        "ambiguous-match",
        "no-match",
        "unauthorized-project",
        "cross-tenant-reference",
        "duplicate",
        "attachment-only",
        "risky-ai-candidate",
        "inbound-authenticity-anomaly",
        "corrected-stale-evidence",
    ];

    /// <summary>Required workflow channels.</summary>
    public static IReadOnlyList<string> RequiredWorkflowChannels { get; } =
    [
        "mailbox-intake",
        "association",
        "authorization",
        "attachment-handling",
        "approval",
        "ai-mediation",
        "command-execution",
        "audit",
    ];

    /// <summary>Required scaffold partitions.</summary>
    public static IReadOnlyList<string> RequiredPartitions { get; } =
    [
        CalibrationPartition,
        HeldOutRegressionPartition,
        AdversarialPartition,
    ];

    /// <summary>
    /// Known threshold-band wire values, derived from the canonical <see cref="ThresholdBand"/> contract enum so the
    /// reserved fixture field cannot fork the established vocabulary (Story 1.13 AC7; "thresholdBand values align with
    /// ThresholdBand"). Values are the enum members' <see cref="EnumMemberAttribute.Value"/> (below/within/above/critical).
    /// </summary>
    public static IReadOnlyList<string> ThresholdBands { get; } =
    [
        .. Enum.GetValues<ThresholdBand>().Select(static band =>
        {
            FieldInfo field = typeof(ThresholdBand).GetField(band.ToString())
                ?? throw new InvalidOperationException($"ThresholdBand member '{band}' was not found.");
            EnumMemberAttribute attribute = field.GetCustomAttribute<EnumMemberAttribute>()
                ?? throw new InvalidOperationException($"ThresholdBand member '{band}' is missing an EnumMember value.");
            return attribute.Value
                ?? throw new InvalidOperationException($"ThresholdBand member '{band}' has a null EnumMember value.");
        }),
    ];
}
