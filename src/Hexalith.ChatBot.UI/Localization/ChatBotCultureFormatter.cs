using System.Globalization;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.UI.Localization;

/// <summary>
/// Display-only culture-aware formatting for ChatBot UI values.
/// </summary>
public sealed class ChatBotCultureFormatter(ChatBotUiTextLocalizer text)
{
    public string FormatActorLabel(ActorType actorType) => text.ActorTypeLabel(actorType);

    public string FormatConfidence(double value)
        => value.ToString("P0", CultureInfo.CurrentCulture);

    public string FormatConfidenceBand(ThresholdBand band) => text.ConfidenceBandLabel(band);

    public string FormatDateTime(DateTimeOffset value)
        => value.ToLocalTime().ToString("f", CultureInfo.CurrentCulture);

    public string FormatNumber(decimal value)
        => value.ToString("N2", CultureInfo.CurrentCulture);

    public string FormatItemCount(int count)
        => text.Get(count == 1 ? ChatBotUiTextKey.ItemCountOne : ChatBotUiTextKey.ItemCountOther, count);

    public static string FormatInvariantIdentifier<T>(T value)
        where T : IFormattable
        => value.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

    public static bool IdentifierEquals(string left, string right)
        => string.Equals(left, right, StringComparison.Ordinal);
}
