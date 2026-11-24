using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// Tracks live-region announcement keys for one UI circuit so repeated polling or re-entry does not re-announce stable feedback.
/// </summary>
public sealed class ChatBotAnnouncementDeduplicationState
{
    private readonly HashSet<string> _announcedKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _latestActivations = new(StringComparer.Ordinal);

    /// <summary>Determines whether to announce a status or the newest activation of a stable status.</summary>
    public bool ShouldAnnounce(string announcementKey, ChatBotAnnouncementDedupRule dedupRule)
        => ShouldAnnounce(announcementKey, dedupRule, activation: 0);

    /// <summary>Tracks the latest activation for the supplied stable key.</summary>
    internal bool ShouldAnnounce(string announcementKey, ChatBotAnnouncementDedupRule dedupRule, long activation)
    {
        if (dedupRule is ChatBotAnnouncementDedupRule.NoLiveAnnouncement)
        {
            return false;
        }

        if (dedupRule is ChatBotAnnouncementDedupRule.OncePerActivation)
        {
            return true;
        }

        string key = $"{dedupRule}:{announcementKey}";
        if (activation == 0)
        {
            return _announcedKeys.Add(key);
        }

        if (_latestActivations.TryGetValue(key, out long latest) && latest >= activation)
        {
            return false;
        }

        _latestActivations[key] = activation;
        _announcedKeys.Add(key);
        return true;
    }
}
