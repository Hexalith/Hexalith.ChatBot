using Microsoft.Playwright;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>
/// Story 13.9 shared real-render fixture: boots the live <see cref="LiveChatBotUiHost"/> (loopback Kestrel) and a
/// single Chromium browser once for the whole test class, mirroring the existing <c>BrowserHarness</c> Chrome
/// resolution / fallback discipline. When no browser is available the tests SKIP with an explicit reason rather
/// than silently taking a string-only fallback — so a green run with <c>Skipped: 0</c> genuinely exercised the
/// real browser path (the <c>chatbot-e2e-nobrowser-fallback-trap</c>).
/// </summary>
public sealed class RealRenderFixture : IAsyncLifetime
{
    public const string NoBrowserSkipReason =
        "Story 13.9 real-render lane requires a real Chromium browser; none was found (set CHROME_EXECUTABLE_PATH "
        + "or install /usr/bin/google-chrome). Skipping rather than taking a non-browser fallback.";

    private LiveChatBotUiHost? _host;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>Gets the loopback base URI (no trailing slash) the browser navigates to.</summary>
    public string BaseUri => (_host ?? throw new InvalidOperationException("Host not initialised.")).BaseUri;

    /// <summary>Gets a value indicating whether a real browser was launched for this run.</summary>
    public bool BrowserAvailable => _browser is not null;

    public async ValueTask InitializeAsync()
    {
        _host = new LiveChatBotUiHost();
        _ = _host.Server; // Force CreateHost so the real app boots on its loopback Kestrel listener.

        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);

        string? chrome = ResolveChromeExecutable();
        if (chrome is not null)
        {
            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                ExecutablePath = chrome,
                Args = ["--no-sandbox", "--disable-dev-shm-usage"],
            }).ConfigureAwait(false);
        }
    }

    /// <summary>Creates a fresh browser context (reduced motion); culture/color-mode are applied per page/navigation.</summary>
    public async Task<IBrowserContext> NewContextAsync()
    {
        IBrowser browser = _browser ?? throw new InvalidOperationException("No browser available.");
        return await browser.NewContextAsync(new() { ReducedMotion = ReducedMotion.Reduce }).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();

        if (_host is not null)
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string? ResolveChromeExecutable()
    {
        string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return File.Exists(configured) ? configured : null;
        }

        const string linuxChrome = "/usr/bin/google-chrome";
        return File.Exists(linuxChrome) ? linuxChrome : null;
    }
}
