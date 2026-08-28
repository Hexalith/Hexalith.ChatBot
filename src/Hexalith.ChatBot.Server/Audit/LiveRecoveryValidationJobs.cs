namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Stable identifiers for the externally scheduled live-recovery evidence jobs.</summary>
internal static class LiveRecoveryValidationJobs
{
    public const string Continuity = "continuity";

    public const string ControlledLossPath = "controlled-loss-path";

    public const string ProjectionRebuild = "projection-rebuild";

    public const string ScopedOutage = "scoped-outage";

    /// <summary>The complete closed job set required by the release evidence gate.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Continuity,
        ControlledLossPath,
        ProjectionRebuild,
        ScopedOutage,
    };
}
