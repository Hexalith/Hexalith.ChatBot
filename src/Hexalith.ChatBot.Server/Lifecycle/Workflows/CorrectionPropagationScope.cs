namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>The correction-propagation scope a deadline is computed for — the M0/M1 metadata-only path versus the M2 path that includes vector reindex.</summary>
internal enum CorrectionPropagationScope
{
    /// <summary>Metadata-only correction propagation (no vector index) — the existing four M0 activities.</summary>
    M0M1,

    /// <summary>Correction propagation that includes the vector-reindex activity (the slower derived-store rebuild).</summary>
    M2,
}
