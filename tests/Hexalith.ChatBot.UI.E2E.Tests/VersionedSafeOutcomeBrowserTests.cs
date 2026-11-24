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
        string? chrome = RealRenderFixture.ResolveChromeExecutable();
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
        await Assertions.Expect(composer.Locator(".chatbot-governed-composer__actions fluent-button"))
            .ToBeEnabledAsync().ConfigureAwait(true);
        await composer.Locator("textarea").FillAsync("Repeated safe submission").ConfigureAwait(true);
        ILocator banner = composer.Locator("[data-chatbot-stable-id$=\"project-conversation-composer-status\"]");
        await page.EvaluateAsync("""
            () => {
                window.safeOutcomeLiveActivations = [];
                new MutationObserver(() => {
                    const banner = document.querySelector('[data-chatbot-stable-id$="project-conversation-composer-status"]');
                    if (!banner || banner.getAttribute('aria-live') !== 'assertive'
                        || banner.getAttribute('role') !== 'alert'
                        || banner.getAttribute('data-chatbot-live-announced') !== 'true') return;
                    const key = banner.getAttribute('data-chatbot-announcement-key');
                    if (key && !window.safeOutcomeLiveActivations.includes(key)) window.safeOutcomeLiveActivations.push(key);
                }).observe(document.body, {subtree: true, childList: true, attributes: true,
                    attributeFilter: ['aria-live', 'role', 'data-chatbot-live-announced', 'data-chatbot-announcement-key']});
            }
            """).ConfigureAwait(true);
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            await composer.Locator(".chatbot-governed-composer__actions fluent-button").ClickAsync().ConfigureAwait(true);
            await Assertions.Expect(banner).ToHaveAttributeAsync("data-chatbot-announcement-key",
                new System.Text.RegularExpressions.Regex($"^project-conversation-composer-[a-f0-9]+-attempt-{attempt}-authorization_denied$")).ConfigureAwait(true);
            await Assertions.Expect(banner).ToContainTextAsync("The operation is not available to this caller.").ConfigureAwait(true);
            await page.WaitForFunctionAsync("attempt => window.safeOutcomeLiveActivations.some(key => key.endsWith(`-attempt-${attempt}-authorization_denied`))", attempt).ConfigureAwait(true);
        }
        (await page.EvaluateAsync<string[]>("() => window.safeOutcomeLiveActivations").ConfigureAwait(true)).Length.ShouldBe(2);
    }

    /// <summary>A deduplicated failure banner remounted in the same circuit has no live role; the instance that announced keeps one.</summary>
    [Fact]
    public async Task RemountedSuppressedBannerShouldDropItsLiveRole()
    {
        LiveChatBotUiHost host = new("authorization_denied");
        await using var hostLifetime = host.ConfigureAwait(true);
        _ = host.Server;
        using IPlaywright playwright = await Playwright.CreateAsync().ConfigureAwait(true);
        string? chrome = RealRenderFixture.ResolveChromeExecutable();
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
        IResponse? response = await page.GotoAsync($"{host.BaseUri}/governed-operations").ConfigureAwait(true);
        response.ShouldNotBeNull();
        response!.Status.ShouldBe(200);
        browser.BrowserType.Name.ShouldBe("chromium");
        browser.Version.ShouldNotBeNullOrWhiteSpace();

        // Only a live browser DOM can record these renders; the document marker proves every remount stays in one circuit.
        await page.EvaluateAsync("""
            () => {
                window.suppressedBannerDocument = crypto.randomUUID();
                window.suppressedBannerRenders = [];
                let mounts = 0;
                new MutationObserver(() => {
                    const banner = document.querySelector('[data-chatbot-stable-id="governed-note-failed"]');
                    if (!banner) return;
                    banner.suppressedBannerMount ??= ++mounts;
                    const render = [banner.suppressedBannerMount, banner.getAttribute('data-chatbot-announcement-key'),
                        banner.getAttribute('data-chatbot-live-announced'), banner.getAttribute('aria-live'),
                        banner.getAttribute('role') ?? 'none'].join('|');
                    if (window.suppressedBannerRenders.at(-1) !== render) window.suppressedBannerRenders.push(render);
                }).observe(document.body, {subtree: true, childList: true, attributes: true,
                    attributeFilter: ['role', 'aria-live', 'data-chatbot-live-announced', 'data-chatbot-announcement-key']});
            }
            """).ConfigureAwait(true);
        string documentMarker = await page.EvaluateAsync<string>("() => window.suppressedBannerDocument").ConfigureAwait(true);

        ILocator record = page.Locator("[data-chatbot-stable-id='record-governed-note'] fluent-button");
        await Assertions.Expect(record).ToBeEnabledAsync(new() { Timeout = 20_000 }).ConfigureAwait(true);
        await record.ClickAsync().ConfigureAwait(true);
        ILocator banner = page.Locator("[data-chatbot-stable-id='governed-note-failed']");
        await Assertions.Expect(banner).ToHaveAttributeAsync("data-chatbot-feedback-state", "TerminalPolicyFailure").ConfigureAwait(true);
        await page.WaitForFunctionAsync("() => window.suppressedBannerRenders.some(render => render.startsWith('1|'))").ConfigureAwait(true);

        ICDPSession cdp = await context.NewCDPSessionAsync(page).ConfigureAwait(true);
        for (int mount = 2; mount <= 3; mount++)
        {
            await page.EvaluateAsync("() => Blazor.navigateTo('/', false)").ConfigureAwait(true);
            await page.Locator("#project-workspace-title").WaitForAsync(new() { State = WaitForSelectorState.Visible }).ConfigureAwait(true);
            await Assertions.Expect(banner).ToHaveCountAsync(0).ConfigureAwait(true);
            await page.EvaluateAsync("() => Blazor.navigateTo('/governed-operations', false)").ConfigureAwait(true);
            await page.WaitForFunctionAsync("mount => window.suppressedBannerRenders.some(render => render.startsWith(`${mount}|`))", mount).ConfigureAwait(true);
            await Assertions.Expect(banner).ToHaveAttributeAsync("aria-live", "off").ConfigureAwait(true);
            (await banner.GetAttributeAsync("role").ConfigureAwait(true)).ShouldBeNull();

            // Chromium keeps an inserted role="alert" an alert even with aria-live off; the remount must expose
            // neither an alert role nor a live region to assistive technology.
            JsonElement node = await AccessibilityNodeAsync(cdp, "[data-chatbot-stable-id='governed-note-failed']").ConfigureAwait(true);
            node.GetProperty("role").GetProperty("value").GetString().ShouldNotBe("alert");
            node.GetProperty("properties").EnumerateArray().Select(property => property.GetProperty("name").GetString()).ShouldNotContain("live");
        }

        (await page.EvaluateAsync<string>("() => window.suppressedBannerDocument").ConfigureAwait(true)).ShouldBe(documentMarker);

        // Each render is "mount|announcement key|announced|aria-live|role".
        const string Key = "governed-note-failed-authorization_denied";
        string[] renders = await page.EvaluateAsync<string[]>("() => window.suppressedBannerRenders").ConfigureAwait(true);
        renders.ShouldContain($"1|{Key}|true|assertive|alert", string.Join(", ", renders));
        renders.ShouldAllBe(render => render.Contains($"|{Key}|", StringComparison.Ordinal), string.Join(", ", renders));
        renders.Where(render => render.StartsWith("1|", StringComparison.Ordinal))
            .ShouldAllBe(render => render.EndsWith("|alert", StringComparison.Ordinal), string.Join(", ", renders));
        foreach (int mount in new[] { 2, 3 })
        {
            string[] remounted = [.. renders.Where(render => render.StartsWith($"{mount}|", StringComparison.Ordinal))];
            remounted.ShouldNotBeEmpty(string.Join(", ", renders));
            remounted.ShouldAllBe(render => render.EndsWith("|false|off|none", StringComparison.Ordinal), string.Join(", ", renders));
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
        string? chrome = RealRenderFixture.ResolveChromeExecutable();
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
        await Assertions.Expect(composer.Locator(".chatbot-governed-composer__actions fluent-button"))
            .ToBeEnabledAsync().ConfigureAwait(true);
        ILocator input = composer.Locator("textarea");
        await input.FillAsync("Safe failure test submission").ConfigureAwait(true);
        await composer.Locator(".chatbot-governed-composer__actions fluent-button")
            .ClickAsync().ConfigureAwait(true);
        ILocator banner = composer.Locator("[data-chatbot-stable-id$=\"project-conversation-composer-status\"]");
        await Assertions.Expect(banner).ToHaveAttributeAsync("data-chatbot-feedback-state",
            code == "authorization_denied" ? "BlockedAction" : "TerminalPolicyFailure").ConfigureAwait(true);
        await Assertions.Expect(banner).ToHaveAttributeAsync("role", "alert").ConfigureAwait(true);
        // Editing the draft re-renders the still-mounted banner: the repeat is not announced, and the role stays.
        await input.FillAsync("Safe failure test submission, edited").ConfigureAwait(true);
        await Assertions.Expect(banner).ToHaveAttributeAsync("data-chatbot-live-announced", "false").ConfigureAwait(true);
        await Assertions.Expect(banner).ToHaveAttributeAsync("aria-live", "off").ConfigureAwait(true);
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
        ILocator action = banner.Locator("[data-chatbot-stable-id$='project-conversation-composer-status-safe-action'] fluent-button");
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

    private static async Task<JsonElement> AccessibilityNodeAsync(ICDPSession cdp, string selector)
    {
        JsonElement? document = await cdp.SendAsync("DOM.getDocument", new() { ["depth"] = 0 }).ConfigureAwait(true);
        JsonElement? node = await cdp.SendAsync("DOM.querySelector", new()
        {
            ["nodeId"] = document!.Value.GetProperty("root").GetProperty("nodeId").GetInt32(),
            ["selector"] = selector,
        }).ConfigureAwait(true);
        JsonElement? tree = await cdp.SendAsync("Accessibility.getPartialAXTree", new()
        {
            ["nodeId"] = node!.Value.GetProperty("nodeId").GetInt32(),
            ["fetchRelatives"] = false,
        }).ConfigureAwait(true);
        return tree!.Value.GetProperty("nodes")[0];
    }
}
