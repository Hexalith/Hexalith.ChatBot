namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Describes one metadata-only gate failure.
/// </summary>
/// <param name="ReasonCode">The stable failure reason.</param>
/// <param name="Subject">The metadata subject that failed.</param>
public sealed record GateIssue(string ReasonCode, string Subject)
{
    /// <summary>Creates an issue with a bounded, non-sensitive subject.</summary>
    public static GateIssue Create(string reasonCode, string subject)
    {
        bool unsafeSubject = string.IsNullOrWhiteSpace(subject)
            || subject.Length > 160
            || subject.Any(char.IsControl)
            || new[] { "secret", "password", "credential", "token", "payload", "prompt", "bearer" }
                .Any(value => subject.Contains(value, StringComparison.OrdinalIgnoreCase));
        return new GateIssue(reasonCode, unsafeSubject ? "redacted" : subject);
    }
}
