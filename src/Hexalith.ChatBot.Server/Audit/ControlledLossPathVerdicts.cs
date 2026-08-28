namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The closed verdict vocabulary for the retained controlled-loss RPO drill.</summary>
internal static class ControlledLossPathVerdicts
{
    /// <summary>The loss path was exercised with valid positive durable bounds within the canonical RPO target.</summary>
    public const string Met = "met";

    /// <summary>The loss path was validly measured but exceeded the canonical RPO target.</summary>
    public const string Missed = "missed";

    /// <summary>The loss path could not produce valid authoritative bounds or violated a safety invariant.</summary>
    public const string Unmeasurable = "unmeasurable";

    /// <summary>Returns whether <paramref name="verdict"/> belongs to the closed vocabulary.</summary>
    public static bool Contains(string? verdict)
        => verdict is Met or Missed or Unmeasurable;
}
