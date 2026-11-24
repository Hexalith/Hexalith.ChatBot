using Hexalith.ChatBot.Client.Generated;

using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // Browser tests keep awaits on the xUnit synchronization context.

/// <summary>Verifies disclosure, activation, and focus on the real association review route.</summary>
public sealed class RealRenderAssociationEvidenceE2ETests
{
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string NextAssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FB0";
    private const string Reference = "evidence-reference-sentinel";
    private const string Fingerprint = "evidence-fingerprint-sentinel";
    private const string EvidenceKind = "evidence-kind-sentinel";
    private const string AvailableChip = "fluent-button[data-chatbot-evidence-state='Available']";
    private const string CandidateChip = ".chatbot-association-candidate__evidence " + AvailableChip;

    /// <summary>Both chip surfaces open the same nonmodal panel and receive focus on dismissal.</summary>
    [Fact]
    public async Task OpensFromCandidateByPointerAndComparisonByKeyboard()
    {
        await using RealRenderFixture fixture = CreateFixture(AssociationEvidenceReferenceVisibilityState.Available);
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAssociationAsync(context, fixture.BaseUri);
        ILocator candidateChip = page.Locator(CandidateChip);
        ILocator comparisonChip = page.Locator("[data-chatbot-association-comparison] " + AvailableChip);
        ILocator panel = page.GetByRole(AriaRole.Region, new() { NameString = "Supporting evidence" });

        await Assertions.Expect(candidateChip).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(panel).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText(Fingerprint, new() { Exact = true })).ToHaveCountAsync(0);

        await candidateChip.ClickAsync();
        await VerifyOpenedAndCloseAsync(page, panel, candidateChip);

        await comparisonChip.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await VerifyOpenedAndCloseAsync(page, panel, comparisonChip);

        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await page.EvaluateAsync("path => Blazor.navigateTo(path, false)", $"/association-review/{NextAssociationId}");
        await Assertions.Expect(page.GetByText(NextAssociationId, new() { Exact = true }).First).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText(Fingerprint, new() { Exact = true })).ToHaveCountAsync(0);
    }

    /// <summary>Removes an open panel when a refreshed association revokes access to its evidence.</summary>
    [Fact]
    public async Task RevokedEvidenceClosesThePanelAfterRefresh()
    {
        await using RealRenderFixture fixture = CreateFixture(
            AssociationEvidenceReferenceVisibilityState.Available, revokeOnSubmission: true);
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAssociationAsync(context, fixture.BaseUri);
        await page.Locator(CandidateChip).ClickAsync();
        ILocator panel = page.GetByRole(AriaRole.Region, new() { NameString = "Supporting evidence" });
        await Assertions.Expect(panel.GetByText(Fingerprint, new() { Exact = true })).ToBeVisibleAsync();

        await page.Locator("fluent-button[aria-label='Choose candidate']").ClickAsync();
        await page.Locator("[data-chatbot-stable-id='association-review-confirm-submit'] fluent-button").ClickAsync();
        await Assertions.Expect(page.Locator("[data-chatbot-evidence-state='Redacted']")).ToHaveCountAsync(2);
        await Assertions.Expect(panel).ToHaveCountAsync(0);
        (await page.ContentAsync()).ShouldNotContain(Fingerprint);
    }

    /// <summary>Restricted statuses cannot disclose any of the reference fields through either surface.</summary>
    [Theory]
    [InlineData(AssociationEvidenceReferenceVisibilityState.Redacted, "Redacted")]
    [InlineData(AssociationEvidenceReferenceVisibilityState.Unavailable, "Unavailable")]
    [InlineData((AssociationEvidenceReferenceVisibilityState)999, "Unavailable")]
    public async Task RestrictedEvidenceStaysNoninteractive(
        AssociationEvidenceReferenceVisibilityState visibility, string expectedState)
    {
        await using RealRenderFixture fixture = CreateFixture(visibility);
        await fixture.InitializeAsync();
        Assert.SkipWhen(!fixture.BrowserAvailable, RealRenderFixture.NoBrowserSkipReason);

        await using IBrowserContext context = await fixture.NewContextAsync();
        IPage page = await OpenAssociationAsync(context, fixture.BaseUri);
        ILocator chips = page.Locator($"[data-chatbot-evidence-state='{expectedState}']");
        await Assertions.Expect(chips).ToHaveCountAsync(2);
        await Assertions.Expect(chips.And(page.Locator("fluent-button, [role='button']"))).ToHaveCountAsync(0);
        await Assertions.Expect(chips.And(page.Locator("[tabindex], [aria-expanded]"))).ToHaveCountAsync(0);

        await chips.First.ClickAsync();
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(page.Locator("#association-evidence-panel")).ToHaveCountAsync(0);
        string html = await page.ContentAsync();
        html.ShouldNotContain(Reference);
        html.ShouldNotContain(Fingerprint);
        html.ShouldNotContain(EvidenceKind);
    }

    private static RealRenderFixture CreateFixture(
        AssociationEvidenceReferenceVisibilityState visibility, bool revokeOnSubmission = false)
    {
        AssociationEvidenceReference evidence = new()
        {
            EvidenceReference = Reference,
            EvidenceFingerprint = Fingerprint,
            EvidenceKind = EvidenceKind,
            VisibilityState = visibility,
            FreshnessState = AssociationEvidenceReferenceFreshnessState.Fresh,
            RedactionState = AssociationEvidenceReferenceRedactionState.Metadata_only,
        };
        return new(() => new FakeChatBotClient
        {
            AssociationEvidenceOverride = evidence,
            SubmissionObserved = revokeOnSubmission
                ? (_, _, _) => evidence.VisibilityState = AssociationEvidenceReferenceVisibilityState.Redacted
                : null,
        });
    }

    private static async Task<IPage> OpenAssociationAsync(IBrowserContext context, string baseUri)
    {
        await context.AddCookiesAsync([new Cookie { Name = ".AspNetCore.Culture", Value = "c=en|uic=en", Url = baseUri }]);
        IPage page = await context.NewPageAsync();
        (await page.GotoAsync($"{baseUri}/association-review/{AssociationId}"))!.Ok.ShouldBeTrue();
        await Assertions.Expect(page.Locator("[data-chatbot-association-candidate]")).ToBeVisibleAsync();
        return page;
    }

    private static async Task VerifyOpenedAndCloseAsync(IPage page, ILocator panel, ILocator opener)
    {
        await Assertions.Expect(panel.GetByText(Reference, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(panel.GetByText(Fingerprint, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(panel.GetByText(EvidenceKind, new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(opener).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(opener).ToHaveAttributeAsync("aria-controls", "association-evidence-panel");

        ILocator close = panel.Locator("fluent-button#association-evidence-close");
        await Assertions.Expect(close).ToHaveAttributeAsync("aria-label", "Close supporting evidence");
        await Assertions.Expect(close).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(panel).ToHaveCountAsync(0);
        await Assertions.Expect(opener).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(opener).ToBeFocusedAsync();
    }
}
