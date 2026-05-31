using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// Tracks live-region announcement keys for one UI circuit so repeated polling or re-entry does not re-announce stable feedback.
/// </summary>
public sealed class ChatBotAnnouncementDeduplicationState
{
    private readonly HashSet<string> _announcedKeys = new(StringComparer.Ordinal);

    public bool ShouldAnnounce(string announcementKey, ChatBotAnnouncementDedupRule dedupRule)
    {
        if (dedupRule is ChatBotAnnouncementDedupRule.NoLiveAnnouncement)
        {
            return false;
        }

        if (dedupRule is ChatBotAnnouncementDedupRule.OncePerActivation)
        {
            return true;
        }

        return _announcedKeys.Add($"{dedupRule}:{announcementKey}");
    }
}
