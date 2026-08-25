using Hexalith.ChatBot.UI.Localization;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Builds a real <see cref="ChatBotUiTextLocalizer"/> over the shipped resources, so tests exercise the same
/// strings the surface renders instead of a stub that would hide a missing key.
/// </summary>
internal static class AssociationReviewTestText
{
    public static ChatBotUiTextLocalizer Create()
    {
        ResourceManagerStringLocalizerFactory factory = new(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        return new ChatBotUiTextLocalizer(new StringLocalizer<SharedResource>(factory));
    }
}
