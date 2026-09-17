namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A single bounded routing-matrix row: the <c>(state-class × scope)</c> key plus the declared recipient role and
/// channel tokens. All values are declared enum tokens, never recipient PII.
/// </summary>
public sealed record ChatBotNotificationRoutingMatrixRow(
    string StateClass,
    string Scope,
    string RecipientRole,
    string Channel);
