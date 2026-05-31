namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Safe correction recovery statuses exposed without restricted target detail.
/// </summary>
public enum ChatBotCorrectionRecoveryStatus
{
    Success,
    Partial,
    Blocked,
    RawFailure,
}
