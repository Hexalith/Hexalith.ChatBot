namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Responsive retention policy for a dense-row field.
/// </summary>
public enum ChatBotDenseRowFieldRetention
{
    /// <summary>The field must stay visible in the collapsed row.</summary>
    MustKeepVisible,

    /// <summary>The field may move to a reachable detail surface if the collapsed row is too dense.</summary>
    MustMoveToDetail,

    /// <summary>The field is a first collapse candidate on phone-sized rows.</summary>
    CollapseFirst,
}
