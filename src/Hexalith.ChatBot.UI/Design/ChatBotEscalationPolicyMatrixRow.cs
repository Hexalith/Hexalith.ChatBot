namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A single bounded escalation-matrix row: the <c>(state-class × scope)</c> key plus the bounded age threshold and
/// the declared severity, escalation-target role, and channel tokens. All values are declared enum tokens or bounded
/// integers, never recipient PII.
/// </summary>
public sealed record ChatBotEscalationPolicyMatrixRow(
    string StateClass,
    string Scope,
    int AgeThresholdSeconds,
    string SeverityThreshold,
    string EscalationTargetRole,
    string EscalationChannel);
