using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;

namespace Hexalith.ChatBot.UI.Localization;

/// <summary>
/// UI-owned culture contract for the ChatBot Blazor host.
/// </summary>
public static class ChatBotSupportedCultures
{
    public const string DefaultCultureName = "en";
    public const string FrenchCultureName = "fr";

    public static IReadOnlyList<string> SupportedCultureNames { get; } = [DefaultCultureName, FrenchCultureName];

    public static IReadOnlyList<CultureInfo> SupportedCultures { get; } =
        SupportedCultureNames.Select(static culture => CultureInfo.GetCultureInfo(culture)).ToArray();

    public static RequestLocalizationOptions CreateRequestLocalizationOptions()
    {
        RequestLocalizationOptions options = new()
        {
            DefaultRequestCulture = new RequestCulture(DefaultCultureName),
            FallBackToParentCultures = true,
            FallBackToParentUICultures = true,
        };

        options.SetDefaultCulture(DefaultCultureName);
        options.AddSupportedCultures([.. SupportedCultureNames]);
        options.AddSupportedUICultures([.. SupportedCultureNames]);
        return options;
    }
}
