namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Safe blocked-state reasons shown by governed ChatBot surfaces.
/// </summary>
public enum ChatBotBlockedReason
{
    Denial,
    UnresolvedAssociation,
    Quarantine,
    FailedDependency,
    UnsafeContext,
}
