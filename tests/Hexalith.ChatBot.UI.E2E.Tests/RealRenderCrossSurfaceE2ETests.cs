using System.Text.RegularExpressions;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods keep awaits on the xUnit synchronization context.

/// <summary>
/// Story 13.9 closing gate: real-render cross-surface re-verification. Boots the actual
/// <c>Hexalith.ChatBot.UI</c> Blazor Server app on a loopback Kestrel listener (see
/// <see cref="LiveChatBotUiHost"/>) and drives a real Chromium browser with <c>Page.GotoAsync</c> against the
/// six routable surfaces — NOT hand-authored <c>Page.SetContentAsync</c> HTML fixtures. It proves, against the
/// live FrontComposer shell + routable Razor components + real Fluent components + real CSS cascade, that the
/// Epic 13 layout-composition fixes hold: every route composes <c>FcPageLayout</c>/<c>FcPageHeader</c> below the
/// 48px shell header band, no legacy <c>.chatbot-*</c> page chrome survives, and a11y/theme/culture/forced-colors
/// behaviour is real. Screenshots and a machine-checked geometry/DOM/a11y matrix are the evidence.
/// </summary>
public sealed class RealRenderCrossSurfaceE2ETests(RealRenderFixture fixture) : IClassFixture<RealRenderFixture>
{
    private readonly RealRenderFixture _fixture = fixture;

    /// <summary>The six live routable surfaces and the metadata needed to verify each one.</summary>
    public static readonly Surface[] Surfaces =
    [
        new("project-workspace", "/", "project-workspace-title"),
        new("project-conversation", "/projects/project-alpha/conversation", "project-conversation-title"),
        new("governed-operations", "/governed-operations", "governed-operations-title"),
        new("operational-dashboards", "/operational-dashboards", "operational-dashboards-title"),
        new("compliance-audit-investigation", "/compliance-audit-investigation", "compliance-audit-title"),
        new("association-review", "/association-review/01ARZ3NDEKTSV4RRFFQ69G5FAW", "association-review-title"),
    ];

    private static readonly ColorMode[] ColorModes =
    [
        new("light", ColorScheme.Light, ForcedColors.None),
        new("dark", ColorScheme.Dark, ForcedColors.None),
        new("forced-colors", ColorScheme.Dark, ForcedColors.Active),
    ];

    private const string EnCulture = "en";
    private const string FrCulture = "fr";
    private const int ShellHeaderBandPx = 48;

    /// <summary>
    /// Story 13.7 grouped surfaces that compose their sibling titled sections inside a real <c>FluentAccordion</c>
    /// and reliably render at least one under the deterministic <see cref="FakeChatBotClient"/> seam. Asserted
    /// per-surface (AC3 / Task 3 "FluentAccordion on 13.7 grouped surfaces") so the specific Fluent composition —
    /// not merely a generic Fluent element — is proven live.
    /// <para>
    /// The data-grid surfaces (operational dashboards / compliance audit) are intentionally NOT asserted for a
    /// <c>FluentDataGrid</c>/<c>FluentGrid</c> here: those grids only materialise once seeded with query results,
    /// and the metadata-only seam holds an Unknown/empty posture, so a strict data-grid assertion would be a false
    /// negative of the test seam rather than of the live composition. The generic Fluent-composition assertion plus
    /// the legacy-<c>&lt;dl&gt;</c>-absence assertion already prove those surfaces are Fluent-composed, not dumped.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> AccordionGroupedSurfaces = ["project-workspace", "governed-operations"];

    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    // AC1/AC3/AC4: real host, legacy chrome absent, FrontComposer composition present below the shell band.
    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AllSixSurfaces_ComposeFrontComposerLayout_WithoutLegacyChrome_BelowShellBand()
    {
        Assert.SkipWhen(!_fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await _fixture.NewContextAsync();
        IPage page = await context.NewPageAsync();

        foreach (Surface surface in Surfaces)
        {
            await OpenAsync(page, surface, EnCulture, ColorModes[0], 1280, 900);

            // AC3 — no legacy page chrome survives anywhere in the live DOM (whole-class-token match, mirroring the
            // ChatBotLayoutCompositionConformanceTests regex so prefixed tokens like chatbot-project-context-header
            // / chatbot-project-workspace are NOT false positives).
            foreach (string banned in new[] { "chatbot-page-header", "chatbot-page", "chatbot-command-bar", "chatbot-definition-list", "chatbot-skip-link" })
            {
                int hits = await page.Locator($"[class~=\"{banned}\"]").CountAsync();
                hits.ShouldBe(0, $"[{surface.Key}] legacy class '.{banned}' must be absent from the live render.");
            }

            // AC3 — no primary-content monospace <dl> data dumps remain in the main content region.
            (await page.Locator("#fc-main-content dl").CountAsync())
                .ShouldBe(0, $"[{surface.Key}] primary-content <dl> dumps must be migrated to Fluent data presentation.");

            // AC3 — real composed structures exist: the FrontComposer shell skip link, the single main landmark,
            // the route heading, and genuine Fluent components inside the content region.
            (await page.Locator("a.fc-skip-link[href=\"#fc-main-content\"]").CountAsync())
                .ShouldBe(1, $"[{surface.Key}] the FrontComposer shell skip link must target #fc-main-content.");
            (await page.Locator("#fc-main-content[role=\"main\"]").CountAsync())
                .ShouldBe(1, $"[{surface.Key}] exactly one #fc-main-content main landmark must exist.");

            ILocator heading = page.Locator($"h1#{surface.HeadingId}");
            (await heading.CountAsync()).ShouldBe(1, $"[{surface.Key}] the FcPageHeader route heading must render exactly once.");
            (await heading.InnerTextAsync()).ShouldNotBeNullOrWhiteSpace($"[{surface.Key}] route heading text must be present.");

            int fluentInMain = await page.Locator(
                "#fc-main-content fluent-card, #fc-main-content fluent-stack, #fc-main-content fluent-text, " +
                "#fc-main-content fluent-data-grid, #fc-main-content fluent-grid, #fc-main-content fluent-accordion, " +
                "#fc-main-content fluent-badge, #fc-main-content fluent-button").CountAsync();
            fluentInMain.ShouldBeGreaterThan(0, $"[{surface.Key}] the content must be composed from real Fluent components.");

            // AC3 — FcPageLayout output: the shell's #fc-main-content carries the FcPageLayout coordinator marker
            // class + measure attribute for every routable surface, proving each page composes THROUGH FcPageLayout
            // (not a hand-rolled .chatbot-page wrapper). FcPageHeader output is already proven by the h1 heading above,
            // so this closes AC3's "real DOM contains FcPageLayout/FcPageHeader output" for both halves.
            (await page.Locator("#fc-main-content.fc-page-layout").CountAsync())
                .ShouldBe(1, $"[{surface.Key}] #fc-main-content must carry the FcPageLayout 'fc-page-layout' marker class.");
            (await page.Locator("#fc-main-content[data-fc-page-layout]").CountAsync())
                .ShouldBe(1, $"[{surface.Key}] #fc-main-content must expose the FcPageLayout data-fc-page-layout measure attribute.");

            // AC1/AC4 — the FrontComposer shell must actually COMPOSE as a FluentLayout CSS grid in the live render.
            // FluentLayout renders a <div class="fluent-layout"> whose grid lives in the scoped CSS bundle; if that
            // bundle never loads, the div collapses to display:block and the 48px header band stacks on top of the
            // page content (the Story 12.9-class shell-overlap defect). Asserting display:grid proves the real
            // scoped CSS cascade loaded, so a collapsed/un-styled render FAILS this gate instead of passing silently
            // with coarse single-element geometry. (Regression guard for the missing-scoped-bundle App.razor fix.)
            string layoutDisplay = await page.EvaluateAsync<string>(
                "() => { const e = document.querySelector('.fluent-layout'); return e ? getComputedStyle(e).display : '(no .fluent-layout)'; }");
            layoutDisplay.ShouldBe("grid", $"[{surface.Key}] the FrontComposer shell must compose as a real FluentLayout CSS grid (scoped layout CSS must load).");

            // AC3 — specific Fluent composition (beyond the generic any-Fluent check): the Story 13.7 grouped surfaces
            // present their sibling titled sections inside a real FluentAccordion in the live render.
            if (AccordionGroupedSurfaces.Contains(surface.Key))
            {
                (await page.Locator("#fc-main-content fluent-accordion").CountAsync())
                    .ShouldBeGreaterThan(0, $"[{surface.Key}] Story 13.7 grouped sections must compose a real FluentAccordion live.");
            }

            // AC4 — geometry: the route heading renders BELOW the 48px shell header band (the shell app-title h1
            // anchors the band) and never intersects it, proving the Story 12.9-era shell overlap is gone.
            LocatorBoundingBoxResult? headingBox = await heading.BoundingBoxAsync();
            headingBox.ShouldNotBeNull($"[{surface.Key}] route heading must have a real layout box.");
            float bandBottom = await ShellHeaderBandBottomAsync(page);
            headingBox!.Y.ShouldBeGreaterThanOrEqualTo(bandBottom, $"[{surface.Key}] heading top must be below the shell header band.");
            headingBox.Y.ShouldBeGreaterThanOrEqualTo(ShellHeaderBandPx, $"[{surface.Key}] heading top must clear the 48px header band.");

            // AC4 — control-anchored non-intersection: the route heading must not intersect the header action cluster
            // (FcThemeToggle / palette / settings / account-menu). Anchor on the always-present, right-most account
            // menu and prove the heading box starts at or below its bottom edge — a direct overlap check against a
            // real action control (Task 3), not only the abstract 48px band line.
            LocatorBoundingBoxResult? accountBox =
                await page.Locator("[data-testid=\"fc-account-menu\"]").First.BoundingBoxAsync();
            accountBox.ShouldNotBeNull($"[{surface.Key}] the shell header account-menu action must render in the header band.");
            headingBox.Y.ShouldBeGreaterThanOrEqualTo(
                accountBox!.Y + accountBox.Height,
                $"[{surface.Key}] heading must not intersect the header action cluster (account/theme/palette/settings).");

            // AC4 — no visible hard 1px bordered page/content box replacing Fluent composition: the main content
            // wrapper must not carry a hand-rolled solid border (the legacy .chatbot-page box look).
            bool mainHasHardBorder = await page.EvaluateAsync<bool>(
                """
                () => {
                  const el = document.querySelector('#fc-main-content');
                  if (!el) return false;
                  const cs = getComputedStyle(el);
                  return cs.borderTopStyle === 'solid' && parseFloat(cs.borderTopWidth) >= 1
                      && cs.borderLeftStyle === 'solid' && parseFloat(cs.borderLeftWidth) >= 1;
                }
                """);
            mainHasHardBorder.ShouldBeFalse($"[{surface.Key}] main content must not be wrapped in a hard 1px bordered box.");

            await ScreenshotAsync(page, surface, EnCulture, ColorModes[0], "desktop");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    // AC2/AC5: every surface visited under EN+FR and light/dark/forced-colors; screenshots + a11y assertions.
    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AllSixSurfaces_RenderAcrossCulturesColorModesAndForcedColors_WithRealA11y()
    {
        Assert.SkipWhen(!_fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await _fixture.NewContextAsync();
        IPage page = await context.NewPageAsync();

        foreach (Surface surface in Surfaces)
        {
            string enHeading = string.Empty;
            string frHeading = string.Empty;

            foreach (string culture in new[] { EnCulture, FrCulture })
            {
                foreach (ColorMode mode in ColorModes)
                {
                    await OpenAsync(page, surface, culture, mode, 1280, 900);

                    // AC5 — exactly one main landmark, named by the route, with the skip-to-content target present.
                    (await page.Locator("#fc-main-content[role=\"main\"]").CountAsync()).ShouldBe(1);
                    string headingText = (await page.Locator($"h1#{surface.HeadingId}").InnerTextAsync()).Trim();
                    headingText.ShouldNotBeNullOrWhiteSpace($"[{surface.Key}/{culture}/{mode.Name}] heading text must survive.");

                    // AC5 — heading order is sane: the route heading is an <h1> and at least one <h1> lives in main.
                    (await page.Locator("#fc-main-content h1").CountAsync())
                        .ShouldBeGreaterThan(0, $"[{surface.Key}/{culture}/{mode.Name}] main must contain an h1.");

                    await ScreenshotAsync(page, surface, culture, mode, "desktop");

                    if (culture == EnCulture && mode.Name == "forced-colors")
                    {
                        // AC5 — under forced-colors/high-contrast: visible focus is not lost and the skip link moves
                        // focus to #fc-main-content; non-color status cues survive (content still renders text).
                        await AssertSkipLinkFocusFlowAsync(page, surface);
                        (await page.Locator("#fc-main-content").InnerTextAsync()).Trim().Length
                            .ShouldBeGreaterThan(0, $"[{surface.Key}] forced-colors content must retain non-color text cues.");
                    }

                    if (mode.Name == "light")
                    {
                        if (culture == EnCulture) { enHeading = headingText; }
                        else { frHeading = headingText; }
                    }
                }
            }

            // AC5 — culture really flips: each surface's localized heading differs between EN and FR.
            enHeading.ShouldNotBeNullOrWhiteSpace();
            frHeading.ShouldNotBeNullOrWhiteSpace();
            frHeading.ShouldNotBe(enHeading, $"[{surface.Key}] FR heading must differ from EN (localized resource output).");

            // AC2 — at least one narrow/mobile-width capture per surface alongside the desktop matrix.
            await OpenAsync(page, surface, EnCulture, ColorModes[0], 390, 844);
            await ScreenshotAsync(page, surface, EnCulture, ColorModes[0], "mobile");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    // AC3: the five layout-composition allowlists and the not-yet-composed backlog stay empty (source authority).
    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public void ConformanceAllowlistsAndBacklog_RemainEmpty()
    {
        string root = SolutionRoot();
        string conformance = File.ReadAllText(Path.Combine(
            root, "tests", "Hexalith.ChatBot.UI.Tests", "ChatBotLayoutCompositionConformanceTests.cs"));

        foreach (string field in new[]
        {
            "PageHeaderChromeAllowlist",
            "PageContentBoxAllowlist",
            "CommandBarAllowlist",
            "DefinitionListAllowlist",
            "NotYetComposedPageBacklog",
        })
        {
            Match match = Regex.Match(conformance, $@"{field}\s*=\s*\[(?<body>.*?)\]\s*;", RegexOptions.Singleline);
            match.Success.ShouldBeTrue($"Could not find the '{field}' declaration in the conformance guard.");
            match.Groups["body"].Value.Trim().ShouldBeEmpty(
                $"Epic 13 layout-composition list '{field}' must stay empty for Story 13.9's closing gate.");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Navigates to a live route under the given culture/color-mode/viewport and waits for a settled render.</summary>
    private async Task OpenAsync(IPage page, Surface surface, string culture, ColorMode mode, int width, int height)
    {
        await page.SetViewportSizeAsync(width, height);
        await page.EmulateMediaAsync(new() { ColorScheme = mode.ColorScheme, ForcedColors = mode.ForcedColors });

        // Blazor Server carries the request culture into the *interactive circuit* only through the localization
        // cookie. The query string sets the culture for the initial prerender GET, but the interactive re-render is
        // owned by the _blazor WebSocket connection, which has no query string — so without the cookie the circuit
        // reverts to the default culture and the live heading silently renders in EN for both cultures. Setting the
        // standard .AspNetCore.Culture cookie (default CookieRequestCultureProvider format) makes both the prerender
        // and the interactive circuit render the requested culture, so the live FR heading is genuinely localized.
        await page.Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = ".AspNetCore.Culture",
                Value = $"c={culture}|uic={culture}",
                Url = _fixture.BaseUri,
            },
        ]);

        string separator = surface.Route.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        string url = $"{_fixture.BaseUri}{surface.Route}{separator}culture={culture}&ui-culture={culture}";
        IResponse? response = await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load });
        response.ShouldNotBeNull($"[{surface.Key}] navigation returned no response.");
        response!.Status.ShouldBeLessThan(400, $"[{surface.Key}] navigation must not return an HTTP error ({url}).");

        // Wait for the Blazor circuit to upgrade the Fluent custom elements (shadow DOM present).
        await page.WaitForFunctionAsync(
            "() => { const e = document.querySelector('fluent-button, fluent-card, fluent-stack, fluent-text'); return !!(e && e.shadowRoot); }",
            null,
            new() { Timeout = 20000 });

        // Wait for the interactive render to settle: the route heading present with a real, below-the-band layout
        // box (avoids capturing the prerender→interactive reflow mid-flight).
        await page.WaitForFunctionAsync(
            $$"""
            () => {
              const h = document.querySelector('h1#{{surface.HeadingId}}');
              if (!h) return false;
              const r = h.getBoundingClientRect();
              return r.height > 0 && r.top >= {{ShellHeaderBandPx}};
            }
            """,
            null,
            new() { Timeout = 20000 });
    }

    /// <summary>The bottom edge (px) of the 48px shell header band, anchored by the shell app-title h1.</summary>
    private static async Task<float> ShellHeaderBandBottomAsync(IPage page)
    {
        LocatorBoundingBoxResult? appTitle = await page.GetByText("Hexalith ChatBot", new() { Exact = true }).First.BoundingBoxAsync();
        appTitle.ShouldNotBeNull("The shell header app title must render in the header band.");
        return appTitle!.Y + appTitle.Height;
    }

    private static async Task AssertSkipLinkFocusFlowAsync(IPage page, Surface surface)
    {
        // Visible focus is not lost: the first Tab stop is the skip link, and on focus it is on-screen (the
        // fc-skip-link:focus rule pulls it into the viewport from its visually-hidden resting position).
        await page.Locator("body").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });
        await page.Keyboard.PressAsync("Tab");
        string focusedClass = await page.EvaluateAsync<string>("() => document.activeElement?.className?.toString() || ''");
        focusedClass.ShouldContain("fc-skip-link", Case.Insensitive,
            $"[{surface.Key}] the first Tab stop must be the skip link (visible focus not lost).");

        LocatorBoundingBoxResult? skipBox = await page.Locator("a.fc-skip-link:focus").BoundingBoxAsync();
        skipBox.ShouldNotBeNull($"[{surface.Key}] the focused skip link must be laid out (visible focus).");
        skipBox!.X.ShouldBeGreaterThanOrEqualTo(0, $"[{surface.Key}] the focused skip link must be on-screen, not off-canvas.");

        // The skip-to-content target exists and is programmatically focusable (tabindex=-1), so the skip link can
        // deliver focus into the main landmark. (Same-page fragment activation is owned by Blazor enhanced nav, so
        // this asserts the focusability contract directly rather than racing the framework's click interception.)
        string targetTabIndex = await page.EvaluateAsync<string>(
            "() => document.querySelector('#fc-main-content')?.getAttribute('tabindex') ?? ''");
        targetTabIndex.ShouldBe("-1", $"[{surface.Key}] #fc-main-content must be a programmatic focus target (tabindex=-1).");

        bool targetReceivesFocus = await page.EvaluateAsync<bool>(
            "() => { const m = document.querySelector('#fc-main-content'); if (!m) return false; m.focus(); return document.activeElement === m; }");
        targetReceivesFocus.ShouldBeTrue($"[{surface.Key}] #fc-main-content must be able to receive focus from the skip link.");
    }

    private static async Task ScreenshotAsync(IPage page, Surface surface, string culture, ColorMode mode, string width)
    {
        string dir = Path.Combine(SolutionRoot(), "_bmad-output", "implementation-artifacts", "tests", "screenshots", "story-13.9");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, $"{surface.Key}.{culture}.{mode.Name}.{width}.png");
        await page.ScreenshotAsync(new() { Path = file });
    }

    private static string SolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("The test must run beneath the ChatBot repository root (Hexalith.ChatBot.slnx).");
        return directory!.FullName;
    }

    /// <summary>A live routable surface: stable key, route (with stable ids), and FcPageHeader heading id.</summary>
    public sealed record Surface(string Key, string Route, string HeadingId);

    private sealed record ColorMode(string Name, ColorScheme ColorScheme, ForcedColors ForcedColors);
}
