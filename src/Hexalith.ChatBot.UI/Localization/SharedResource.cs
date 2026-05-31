using System.Resources;

namespace Hexalith.ChatBot.UI.Localization;

/// <summary>
/// Marker type for UI-owned shared localization resources.
/// </summary>
public sealed class SharedResource
{
    public static ResourceManager ResourceManager { get; } =
        new("Hexalith.ChatBot.UI.Localization.SharedResource", typeof(SharedResource).Assembly);
}
