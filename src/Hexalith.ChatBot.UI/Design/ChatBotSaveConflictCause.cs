namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Safe tenant-configuration save conflict causes.
/// </summary>
public enum ChatBotSaveConflictCause
{
    None,
    Policy,
    Permission,
    StaleData,
    RawException,
}
