using System.Text.Json;

using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Exercises safe catalog failures in the real Blazor UI with Fluent UI v5 and a Chromium browser.</summary>
public sealed class VersionedSafeOutcomeBrowserTests
{
    /// <summary>Distinct user submissions must announce even when their safe rejection code is unchanged.</summary>
    [Fact]
    public async Task TwoSameCodeSubmissionsShouldUseDistinctAnnouncementKeys()
    {
        LiveChatBotUiHost host = new("authorization_denied");
        await using var hostLifetime = host.ConfigureAwait(true);
        _ = host.Server;
        using IPlaywright playwright = await Playwright.CreateAsync().ConfigureAwait(true);
        string? chrome = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
        IBrowser browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true, ExecutablePath = string.IsNullOrWhiteSpace(chrome) ? null : chrome,
            Args = ["--no-sandbox", "--disable-dev-shm-usage"],
        }).ConfigureAwait(true);
        await using var browserLifetime = browser.ConfigureAwait(true);
        IBrowserContext context = await browser.NewContextAsync().ConfigureAwait(true);
        await using var contextLifetime = context.ConfigureAwait(true);
        await context.AddCookiesAsync([new Cookie { Name = ".AspNetCore.Culture", Value = "c=en|uic=en", Url = host.BaseUri }]).ConfigureAwait(true);
        IPage page = await context.NewPageAsync().ConfigureAwait(true);
        await page.GotoAsync($"{host.BaseUri}/projects/project-alpha/conversation").ConfigureAwait(true);
        ILocator composer = page.Locator(".chatbot-governed-composer");
        await composer.Locator("textarea").FillAsync("Repeated safe submission").ConfigureAwait(true);
        ILocator banner = composer.Locator("#project-conversation-composer-status");
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            await composer.Locator(".chatbot-governed-composer__actions fluent-button").ClickAsync().ConfigureAwait(true);
            await Assertions.Expect(banner).ToHaveAttributeAsync("data-chatbot-announcement-key",
                $"project-conversation-composer-attempt-{attempt}-authorization_denied").ConfigureAwait(true);
            await Assertions.Expect(banner).ToContainTextAsync("The operation is not available to this caller.").ConfigureAwait(true);
        }
    }

    /// <summary>Checks localized denial text, semantic state and keyboard reachable unavailable actions in each visual mode.</summary>
    /// <param name="culture">The UI culture.</param>
    /// <param name="colorScheme">The preferred color scheme.</param>
    /// <param name="forcedColors">The forced colors preference.</param>
    /// <param name="code">The backend failure code.</param>
    [Theory]
    [InlineData("en", ColorScheme.Light, ForcedColors.None, "authorization_denied")]
    [InlineData("en", ColorScheme.Dark, ForcedColors.None, "authorization_denied")]
    [InlineData("en", ColorScheme.Dark, ForcedColors.Active, "authorization_denied")]
    [InlineData("fr", ColorScheme.Light, ForcedColors.None, "authorization_denied")]
    [InlineData("fr", ColorScheme.Dark, ForcedColors.None, "authorization_denied")]
    [InlineData("fr", ColorScheme.Dark, ForcedColors.Active, "authorization_denied")]
    [InlineData("en", ColorScheme.Light, ForcedColors.None, "restricted_unknown_failure")]
    [InlineData("en", ColorScheme.Dark, ForcedColors.None, "restricted_unknown_failure")]
    [InlineData("en", ColorScheme.Dark, ForcedColors.Active, "restricted_unknown_failure")]
    [InlineData("fr", ColorScheme.Light, ForcedColors.None, "restricted_unknown_failure")]
    [InlineData("fr", ColorScheme.Dark, ForcedColors.None, "restricted_unknown_failure")]
    [InlineData("fr", ColorScheme.Dark, ForcedColors.Active, "restricted_unknown_failure")]
    public async Task RejectedComposerShouldRemainSafeAndAccessibleAcrossVisualModes(
        string culture, ColorScheme colorScheme, ForcedColors forcedColors, string code)
    {
        LiveChatBotUiHost host = new(code);
        await using var hostLifetime = host.ConfigureAwait(true);
        _ = host.Server;
        using IPlaywright playwright = await Playwright.CreateAsync().ConfigureAwait(true);
        string? chrome = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
        IBrowser browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            ExecutablePath = string.IsNullOrWhiteSpace(chrome) ? null : chrome,
            Args = ["--no-sandbox", "--disable-dev-shm-usage"],
        }).ConfigureAwait(true);
        await using var browserLifetime = browser.ConfigureAwait(true);
        IBrowserContext context = await browser.NewContextAsync(new()
        {
            ColorScheme = colorScheme,
            ForcedColors = forcedColors,
            ReducedMotion = ReducedMotion.Reduce,
            ViewportSize = new() { Width = 1280, Height = 900 },
        }).ConfigureAwait(true);
        await using var contextLifetime = context.ConfigureAwait(true);
        await context.AddCookiesAsync([new Cookie
        {
            Name = ".AspNetCore.Culture",
            Value = $"c={culture}|uic={culture}",
            Url = host.BaseUri,
        }]).ConfigureAwait(true);
        string themeKey = StorageKeys.BuildKey("tenant-alpha", "actor-alpha", "theme");
        string themeValue = JsonSerializer.Serialize(colorScheme == ColorScheme.Dark ? ThemeValue.Dark : ThemeValue.Light);
        await context.AddInitScriptAsync(
            $"if (location.protocol === 'http:') localStorage.setItem({JsonSerializer.Serialize(themeKey)}, {JsonSerializer.Serialize(themeValue)});")
            .ConfigureAwait(true);
        IPage page = await context.NewPageAsync().ConfigureAwait(true);
        page.SetDefaultTimeout(10000);
        IResponse? response = await page.GotoAsync($"{host.BaseUri}/projects/project-alpha/conversation").ConfigureAwait(true);
        response.ShouldNotBeNull();
        response!.Status.ShouldBe(200);

        string themeMenuLabel = culture == "fr" ? "Changer de thème" : "Change theme";
        string selectedTheme = colorScheme == ColorScheme.Dark
            ? culture == "fr" ? "Sombre" : "Dark"
            : culture == "fr" ? "Clair" : "Light";
        ILocator themeMenu = page.GetByRole(AriaRole.Button, new() { Name = themeMenuLabel });
        await Assertions.Expect(themeMenu).ToContainTextAsync(selectedTheme).ConfigureAwait(true);

        ILocator composer = page.Locator(".chatbot-governed-composer");
        ILocator input = composer.Locator("textarea");
        await input.FillAsync("Safe failure test submission").ConfigureAwait(true);
        await composer.Locator(".chatbot-governed-composer__actions fluent-button")
            .ClickAsync().ConfigureAwait(true);
        ILocator banner = composer.Locator("#project-conversation-composer-status");
        await Assertions.Expect(banner).ToHaveAttributeAsync("data-chatbot-feedback-state", "TerminalPolicyFailure").ConfigureAwait(true);
        // A previously announced persistent state may have aria-live=off after a render; its semantic role stays an alert.
        await Assertions.Expect(banner).ToHaveAttributeAsync("role", "alert").ConfigureAwait(true);
        string reason = code == "authorization_denied"
            ? culture == "fr" ? "L'opération n'est pas disponible pour cet appelant." : "The operation is not available to this caller."
            : culture == "fr" ? "L'action demandée est indisponible." : "The requested action is unavailable.";
        (await banner.InnerTextAsync().ConfigureAwait(true)).ShouldContain(reason);
        string actionLabel = code == "authorization_denied"
            ? culture == "fr" ? "Demander l'accès" : "Request access"
            : culture == "fr" ? "Fermer ce message" : "Dismiss";
        ILocator actionButton = banner.GetByRole(AriaRole.Button, new() { Name = actionLabel, Exact = false });
        await Assertions.Expect(actionButton).ToBeDisabledAsync().ConfigureAwait(true);
        ILocator action = banner.Locator("[data-chatbot-stable-id='project-conversation-composer-status-safe-action'] fluent-button");
        await Assertions.Expect(action).ToHaveAttributeAsync("aria-disabled", "true").ConfigureAwait(true);
        string? reasonId = await action.GetAttributeAsync("aria-describedby").ConfigureAwait(true);
        reasonId.ShouldNotBeNullOrWhiteSpace();
        (await page.Locator($"#{reasonId}").InnerTextAsync().ConfigureAwait(true)).ShouldNotBeNullOrWhiteSpace();
        (await action.EvaluateAsync<int>("element => element.tabIndex").ConfigureAwait(true)).ShouldBeGreaterThanOrEqualTo(0);
        await action.FocusAsync().ConfigureAwait(true);
        await page.Keyboard.PressAsync("Shift+Tab").ConfigureAwait(true);
        await page.Keyboard.PressAsync("Tab").ConfigureAwait(true);
        (await action.EvaluateAsync<bool>("element => document.activeElement === element").ConfigureAwait(true)).ShouldBeTrue();
        await page.Keyboard.PressAsync("Enter").ConfigureAwait(true);
        await Assertions.Expect(action).ToHaveAttributeAsync("aria-disabled", "true").ConfigureAwait(true);
        string accessibility = await banner.AriaSnapshotAsync().ConfigureAwait(true);
        accessibility.ShouldContain(reason);
        accessibility.ShouldContain(actionLabel);
        string content = await page.ContentAsync().ConfigureAwait(true);
        content.ShouldNotContain("restricted_unknown_failure");
        content.ShouldNotContain("restricted prose");
        content.ShouldNotContain("restricted payload");
        content.ShouldNotContain("restricted exception");
    }
}
