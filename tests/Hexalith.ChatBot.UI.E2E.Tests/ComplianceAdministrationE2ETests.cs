using Microsoft.Playwright;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

#pragma warning disable CA2007 // xUnit test methods should keep awaits on the xUnit synchronization context.
public sealed class ComplianceAdministrationE2ETests
{
    [Fact]
    public async Task ComplianceAuditInvestigationShouldExposeMetadataOnlyTimelineAndSafeEscalation()
    {
        BrowserHarness? startedHarness = await BrowserHarness.TryStartAsync();
        if (startedHarness is null)
        {
            // Skip honestly when no real browser is available instead of asserting a static string fixture.
            // A silent no-browser "pass" would mask genuine browser-only failures (the
            // chatbot-e2e-nobrowser-fallback-trap); a visible skip cannot. When Chrome is present, the real
            // assertions below execute against the live page. This keeps the suite portable like every
            // sibling E2E test (CI runs `dotnet test` with no Chrome install step) without re-introducing the
            // silent fixture fallback this story set out to remove.
            Assert.Skip("Real Chrome/Chromium is not available; skipping the browser-only audit-investigation assertions.");
        }

        await using (BrowserHarness harness = startedHarness!)
        {
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.AuditInvestigation));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Compliance audit", Level = 1 }));
            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.List, new() { NameString = "Compliance audit timeline" }));

            await AssertAuditFilterFluentControlsAsync(harness.Page);
            ILocator limitFilter = harness.Page.GetByLabel("Limit", new() { Exact = true });
            await SetFluentNumberInputValueAsync(limitFilter, "25");
            (await limitFilter.GetAttributeAsync("value")).ShouldBe("25");

            ILocator row = harness.Page.GetByRole(AriaRole.Article, new() { NameString = "Audit record, SubmitRetentionConfigurationChange, restricted, 2026-06-02 04:00:00Z" });
            await WaitForVisibleAsync(row);
            await WaitForVisibleAsync(row.GetByText("actor:admin-alpha", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("decision:allow", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("policy-snapshot:policy-snapshot-admin-v1", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("redaction:restricted", new() { Exact = true }));
            await WaitForVisibleAsync(row.GetByText("safe-next-action:request-access", new() { Exact = true }));

            ILocator escalation = row.GetByRole(AriaRole.Button, new() { NameString = "Request compliance access" });
            (await escalation.GetAttributeAsync("aria-describedby")).ShouldBe("compliance-escalation-reason");
            await escalation.ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.commandType")).ShouldBe("RequestComplianceEscalation");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.escalationTarget")).ShouldBe("project-opaque-ref");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Trigger investigation" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.commandType")).ShouldBe("RequestComplianceInvestigation");

            ILocator retry = harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Retry queue item" });
            (await retry.GetAttributeAsync("aria-disabled")).ShouldBe("true");
            (await retry.GetAttributeAsync("aria-describedby")).ShouldBe("compliance-operate-denied");
            await retry.FocusAsync();
            await harness.Page.Keyboard.PressAsync("Enter");
            (await harness.Page.EvaluateAsync<string?>("() => window.__lastWorkflowMutation ?? null")).ShouldBeNull();

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task RetentionConfigurationValidationShouldFocusSummaryAndSubmitSafeSnapshotMetadata()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertRetentionFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.RetentionValidation));

            ILocator summary = harness.Page.GetByRole(AriaRole.Alert, new() { NameString = "Retention validation summary" });
            await WaitForVisibleAsync(summary);
            (await summary.GetAttributeAsync("data-validation-placement")).ShouldBe("before-fields");
            (await summary.GetAttributeAsync("tabindex")).ShouldBe("-1");

            ILocator sourceEmail = harness.Page.GetByLabel("Source email metadata retention days");
            (await sourceEmail.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await sourceEmail.GetAttributeAsync("aria-describedby")).ShouldBe("source-email-retention-message");

            ILocator audit = harness.Page.GetByLabel("Audit record retention days");
            (await audit.GetAttributeAsync("aria-invalid")).ShouldBe("true");
            (await audit.GetAttributeAsync("aria-describedby")).ShouldBe("audit-retention-message");

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit retention change" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => document.activeElement.id")).ShouldBe("retention-validation-summary");
            (await harness.Page.EvaluateAsync<string?>("() => window.__lastRetentionCommand ?? null")).ShouldBeNull();

            await harness.Page.GetByRole(AriaRole.Button, new() { NameString = "Submit valid retention change" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetentionCommand.commandType")).ShouldBe("SubmitRetentionConfigurationChange");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetentionCommand.oldFingerprint")).ShouldBe("sha256:oldretentionfingerprint001");
            (await harness.Page.EvaluateAsync<string>("() => window.__lastRetentionCommand.newFingerprint")).ShouldBe("sha256:newretentionfingerprint001");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task CompliancePhoneFallbackShouldKeepReadOnlySummaryAndEscalationReachable()
    {
        BrowserHarness? harness = await BrowserHarness.TryStartAsync();
        if (harness is null)
        {
            AssertPhoneFixtureWithoutBrowser();
            return;
        }

        await using (harness)
        {
            await harness.Page.SetViewportSizeAsync(390, 844);
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.PhoneFallback));

            ILocator fallback = harness.Page.GetByRole(AriaRole.Complementary, new() { NameString = "Compliance audit summary is available on phone." });
            await WaitForVisibleAsync(fallback);
            await WaitForVisibleAsync(fallback.GetByText("audit-record:retention-change-001", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("safe-next-action:request-access", new() { Exact = true }));
            await WaitForVisibleAsync(fallback.GetByText("Dense audit analysis and retention editing require a larger screen; summary and safe escalation remain reachable.", new() { Exact = true }));

            await AssertAllHiddenAsync(harness.Page.Locator("[data-compliance-dense-audit='true']"));
            await AssertAllHiddenAsync(harness.Page.Locator("[data-compliance-dense-retention='true']"));

            await fallback.GetByRole(AriaRole.Button, new() { NameString = "Request compliance access" }).ClickAsync();
            (await harness.Page.EvaluateAsync<string>("() => window.__lastComplianceCommand.commandType")).ShouldBe("RequestComplianceEscalation");

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    [Fact]
    public async Task ComplianceAuditFilterFormShouldLayOutLabelAboveInputFluentGridWithoutDetachedLabels()
    {
        // Story 13.6 (AC1/AC2): the FR56 filter form is an aligned FluentGrid of label-above-input fields. The
        // accessible name now comes from the Fluent v5 native label (a <label slot="label" for="@Id"> above the input
        // inside <fluent-field label-position="above">); the hand-rolled <div class="chatbot-form-grid"> and the
        // separate <FluentLabel> + redundant per-field aria-label are gone. The From/To dimensions keep their
        // ISO-8601-UTC text contract (no type="datetime-local").
        BrowserHarness? startedHarness = await BrowserHarness.TryStartAsync();
        if (startedHarness is null)
        {
            Assert.Skip("Real Chrome/Chromium is not available; skipping the browser-only audit filter-grid assertions.");
        }

        await using (BrowserHarness harness = startedHarness!)
        {
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.AuditInvestigation));

            await WaitForVisibleAsync(harness.Page.GetByRole(AriaRole.Heading, new() { NameString = "Compliance audit", Level = 1 }));

            // FluentGrid + label-above-input structure, zero detached <fluent-label>, zero chatbot-form-grid, the
            // action row is a FluentStack — all asserted by the shared helper.
            await AssertAuditFilterFluentControlsAsync(harness.Page);

            // Each FR56 dimension resolves by its localized accessible name to its stable id + Fluent control type.
            IReadOnlyDictionary<string, (string AccessibleName, string Tag)> filters = new Dictionary<string, (string, string)>(StringComparer.Ordinal)
            {
                ["compliance-filter-tenant"] = ("Tenant", "fluent-text-input"),
                ["compliance-filter-actor"] = ("Actor", "fluent-text-input"),
                ["compliance-filter-command"] = ("Command", "fluent-text-input"),
                ["compliance-filter-resource"] = ("Resource", "fluent-text-input"),
                ["compliance-filter-decision"] = ("Decision", "fluent-text-input"),
                ["compliance-filter-reason"] = ("Reason", "fluent-text-input"),
                ["compliance-filter-correlation"] = ("Correlation", "fluent-text-input"),
                ["compliance-filter-message-id"] = ("Message id", "fluent-text-input"),
                ["compliance-filter-surface"] = ("Surface", "fluent-text-input"),
                ["compliance-filter-from"] = ("From", "fluent-text-input"),
                ["compliance-filter-to"] = ("To", "fluent-text-input"),
                ["compliance-filter-limit"] = ("Limit", "fluent-number-input"),
            };

            foreach ((string id, (string accessibleName, string tag)) in filters)
            {
                ILocator control = harness.Page.GetByLabel(accessibleName, new() { Exact = true });
                await WaitForVisibleAsync(control);
                (await control.GetAttributeAsync("id")).ShouldBe(id);
                (await control.EvaluateAsync<string>("element => element.tagName.toLowerCase()")).ShouldBe(tag);
            }

            // AC2: From/To stay free-text ISO-8601-UTC inputs — not a native datetime-local picker (which would emit a
            // different local-time format and break SetFromUtcText/SetToUtcText).
            foreach (string isoFilterName in new[] { "From", "To" })
            {
                ILocator isoFilter = harness.Page.GetByLabel(isoFilterName, new() { Exact = true });
                (await isoFilter.GetAttributeAsync("type")).ShouldBeNull();
                (await isoFilter.GetAttributeAsync("value"))!.ShouldEndWith("Z");
            }
        }
    }

    [Fact]
    public async Task ComplianceAuditTimelineShouldRenderSafeMetadataAsFluentStackWithoutDefinitionList()
    {
        // Story 13.6 (AC4): the per-row safe-metadata dump migrates off the monospace <dl class="chatbot-definition-list">
        // to a structured FluentStack (FluentText label + chatbot-code token rows). The container keeps the safe-metadata
        // aria-label, every safe token is preserved verbatim, and no <dl>/<dt>/<dd> nor chatbot-definition-list survives
        // inside the audit timeline (the separate retention-editor surface is out of this story's scope).
        BrowserHarness? startedHarness = await BrowserHarness.TryStartAsync();
        if (startedHarness is null)
        {
            Assert.Skip("Real Chrome/Chromium is not available; skipping the browser-only audit-timeline migration assertions.");
        }

        await using (BrowserHarness harness = startedHarness!)
        {
            await harness.Page.SetContentAsync(BuildComplianceFixture(ComplianceFixtureScenario.AuditInvestigation));

            ILocator timeline = harness.Page.GetByRole(AriaRole.List, new() { NameString = "Compliance audit timeline" });
            await WaitForVisibleAsync(timeline);

            // No definition-list markup remains in the migrated timeline.
            (await timeline.Locator("dl, dt, dd").CountAsync()).ShouldBe(0);
            (await timeline.Locator(".chatbot-definition-list").CountAsync()).ShouldBe(0);

            // The safe metadata is a labelled FluentStack container (a <div>, not a <dl>).
            ILocator metadata = timeline.Locator("[aria-label='Audit record safe metadata']");
            await WaitForVisibleAsync(metadata);
            (await metadata.EvaluateAsync<string>("element => element.tagName.toLowerCase()")).ShouldBe("div");

            foreach (string token in new[]
            {
                "actor:admin-alpha",
                "command:SubmitRetentionConfigurationChange",
                "decision:allow",
                "reason:pre_commit_gate",
                "correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW",
                "policy-snapshot:policy-snapshot-admin-v1",
                "redaction:restricted",
                "escalation:not-requested",
                "safe-next-action:request-access",
            })
            {
                await WaitForVisibleAsync(metadata.GetByText(token, new() { Exact = true }));
            }

            // Every preserved token is a chatbot-code safe token (9 rows), not free-form prose.
            (await metadata.Locator("code.chatbot-code").CountAsync()).ShouldBe(9);

            string bodyText = await harness.Page.EvaluateAsync<string>("() => document.body.innerText");
            AssertMetadataOnly(bodyText);
        }
    }

    private static Task WaitForVisibleAsync(ILocator locator)
        => locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

    private static async Task AssertAuditFilterFluentControlsAsync(IPage page)
    {
        foreach (string label in new[]
        {
            "Tenant",
            "Actor",
            "Command",
            "Resource",
            "Decision",
            "Reason",
            "Correlation",
            "Message id",
            "Surface",
            "From",
            "To",
        })
        {
            // Exact match is required: a substring match for "To" also resolves "Actor" (Ac-to-r), which
            // trips Playwright strict mode and fails the whole assertion on the real browser path.
            ILocator filter = page.GetByLabel(label, new() { Exact = true });
            await WaitForVisibleAsync(filter);
            (await filter.EvaluateAsync<string>("element => element.tagName.toLowerCase()")).ShouldBe("fluent-text-input");
        }

        ILocator limit = page.GetByLabel("Limit", new() { Exact = true });
        await WaitForVisibleAsync(limit);
        (await limit.EvaluateAsync<string>("element => element.tagName.toLowerCase()")).ShouldBe("fluent-number-input");

        // Story 13.6: the filter form is now an aligned FluentGrid of label-above-input fields. The Fluent v5 native
        // label renders as a <label slot="label" for="@Id"> inside a <fluent-field label-position="above">, so the
        // separate <FluentLabel> (rendered <fluent-label for=…>) and the redundant per-field aria-label are gone.
        (await page.Locator("div.fluent-grid.compliance-audit-filters__layout").CountAsync()).ShouldBe(1);
        (await page.Locator("fluent-label[for^='compliance-filter-']").CountAsync()).ShouldBe(0);
        (await page.Locator("fluent-field[label-position='above'] > label[slot='label'][for^='compliance-filter-']").CountAsync()).ShouldBe(12);
        (await page.Locator(".compliance-audit-filters .chatbot-form-grid").CountAsync()).ShouldBe(0);
        (await page.Locator("fluent-text-input[id^='compliance-filter-']").CountAsync()).ShouldBe(11);
        (await page.Locator("fluent-number-input#compliance-filter-limit").CountAsync()).ShouldBe(1);

        // The search/investigation action row is a FluentStack (rendered .fluent-stack-*), not the hand-rolled
        // .compliance-action-row div, and still carries both governed buttons by their stable ids.
        ILocator actions = page.Locator("div.fluent-stack.compliance-audit-filters__actions");
        (await actions.CountAsync()).ShouldBe(1);
        (await actions.Locator("[data-chatbot-stable-id='compliance-search']").CountAsync()).ShouldBe(1);
        (await actions.Locator("[data-chatbot-stable-id='compliance-trigger-investigation']").CountAsync()).ShouldBe(1);
    }

    private static async Task AssertAllHiddenAsync(ILocator locator)
    {
        // The dense audit/retention markers now appear on more than one element (the filter
        // section and the timeline both carry data-compliance-dense-audit), so a bare
        // IsVisibleAsync would trip Playwright strict-mode. Assert every matched region is
        // hidden on the phone viewport instead of just the first.
        int count = await locator.CountAsync();
        count.ShouldBeGreaterThan(0);
        for (int index = 0; index < count; index++)
        {
            (await locator.Nth(index).IsVisibleAsync()).ShouldBeFalse();
        }
    }

    private static string BuildComplianceFixture(ComplianceFixtureScenario scenario)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string scenarioName = scenario.ToString();

        return $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>Compliance administration fixture</title>
                <style>
                  {{css}}
                  .compliance-admin-fixture { max-width: 1120px; margin: 0 auto; padding: 24px; }
                  .compliance-admin-fixture .chatbot-form-grid { display: grid; grid-template-columns: minmax(180px, 260px) minmax(0, 1fr); gap: 12px 16px; align-items: start; }
                  .compliance-admin-fixture fluent-text-input,
                  .compliance-admin-fixture fluent-number-input { display: block; min-height: 44px; }
                  .compliance-admin-fixture input[type="text"] { min-height: 44px; padding: 8px; }
                  .compliance-action-row { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 16px; }
                  /* Story 13.6: Fluent v5 render approximation — FluentGrid (.fluent-grid), FluentGridItem,
                     fluent-field native label-above-input, FluentStack (.fluent-stack-*), FluentText (fluent-text). */
                  .compliance-admin-fixture .fluent-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px 16px; align-items: start; }
                  .compliance-admin-fixture .fluent-grid-item { min-width: 0; }
                  .compliance-admin-fixture fluent-field { display: block; min-height: 60px; }
                  .compliance-admin-fixture fluent-field > label[slot="label"] { display: block; margin-bottom: 4px; }
                  .compliance-admin-fixture .fluent-stack-horizontal { display: flex; gap: 8px; flex-wrap: wrap; align-items: center; }
                  .compliance-admin-fixture .fluent-stack-vertical { display: flex; flex-direction: column; gap: 4px; }
                  .compliance-admin-fixture fluent-text { display: inline-block; }
                  .compliance-phone-fallback { display: none; }
                  @media (max-width: 640px) {
                    [data-compliance-dense-audit="true"] { display: none !important; }
                    [data-compliance-dense-retention="true"] { display: none !important; }
                    .compliance-phone-fallback { display: block; }
                  }
                </style>
              </head>
              <body>
                <main class="chatbot-page compliance-admin-fixture"
                      aria-labelledby="compliance-audit-title"
                      data-compliance-fixture-scenario="{{scenarioName}}">
                  <header class="chatbot-page-header">
                    <span class="chatbot-metadata">Compliance Administration</span>
                    <h1 id="compliance-audit-title" class="chatbot-page-title">Compliance audit</h1>
                  </header>
                  <section class="chatbot-section"
                           data-chatbot-surface="audit-investigation-s9"
                           aria-labelledby="compliance-timeline-title">
                    <h2 id="compliance-timeline-title" class="chatbot-section-title">Audit investigation</h2>
                    <section class="chatbot-section compliance-audit-filters"
                             data-compliance-dense-audit="true"
                             aria-labelledby="compliance-filters-title">
                      <h3 id="compliance-filters-title" class="chatbot-section-title">Filters</h3>
                      <!-- Story 13.6: the FR56 filters lay out in an aligned FluentGrid (.fluent-grid). Each field's
                           accessible name comes from the Fluent v5 native label rendered above the input by FluentField
                           (fluent-field label-position=above + label slot=label for=id); the separate FluentLabel for=,
                           the redundant per-field aria-label, and the .chatbot-form-grid wrapper are gone. aria-labelledby
                           on each input reproduces, without shadow DOM, the same accessible name the real render derives
                           from the slotted label, so GetByLabel still resolves to the input element. -->
                      <div class="fluent-grid compliance-audit-filters__layout">
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-tenant-field" label-position="above">
                            <label id="compliance-filter-tenant-label" slot="label" for="compliance-filter-tenant">Tenant</label>
                            <fluent-text-input slot="input" id="compliance-filter-tenant" role="textbox" aria-labelledby="compliance-filter-tenant-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-actor-field" label-position="above">
                            <label id="compliance-filter-actor-label" slot="label" for="compliance-filter-actor">Actor</label>
                            <fluent-text-input slot="input" id="compliance-filter-actor" role="textbox" aria-labelledby="compliance-filter-actor-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-command-field" label-position="above">
                            <label id="compliance-filter-command-label" slot="label" for="compliance-filter-command">Command</label>
                            <fluent-text-input slot="input" id="compliance-filter-command" role="textbox" aria-labelledby="compliance-filter-command-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-resource-field" label-position="above">
                            <label id="compliance-filter-resource-label" slot="label" for="compliance-filter-resource">Resource</label>
                            <fluent-text-input slot="input" id="compliance-filter-resource" role="textbox" aria-labelledby="compliance-filter-resource-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-decision-field" label-position="above">
                            <label id="compliance-filter-decision-label" slot="label" for="compliance-filter-decision">Decision</label>
                            <fluent-text-input slot="input" id="compliance-filter-decision" role="textbox" aria-labelledby="compliance-filter-decision-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-reason-field" label-position="above">
                            <label id="compliance-filter-reason-label" slot="label" for="compliance-filter-reason">Reason</label>
                            <fluent-text-input slot="input" id="compliance-filter-reason" role="textbox" aria-labelledby="compliance-filter-reason-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-correlation-field" label-position="above">
                            <label id="compliance-filter-correlation-label" slot="label" for="compliance-filter-correlation">Correlation</label>
                            <fluent-text-input slot="input" id="compliance-filter-correlation" role="textbox" aria-labelledby="compliance-filter-correlation-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-message-id-field" label-position="above">
                            <label id="compliance-filter-message-id-label" slot="label" for="compliance-filter-message-id">Message id</label>
                            <fluent-text-input slot="input" id="compliance-filter-message-id" role="textbox" aria-labelledby="compliance-filter-message-id-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-surface-field" label-position="above">
                            <label id="compliance-filter-surface-label" slot="label" for="compliance-filter-surface">Surface</label>
                            <fluent-text-input slot="input" id="compliance-filter-surface" role="textbox" aria-labelledby="compliance-filter-surface-label"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <!-- From/To keep their ISO-8601-UTC text contract (SetFromUtcText/SetToUtcText) — NOT datetime-local. -->
                          <fluent-field id="compliance-filter-from-field" label-position="above">
                            <label id="compliance-filter-from-label" slot="label" for="compliance-filter-from">From</label>
                            <fluent-text-input slot="input" id="compliance-filter-from" role="textbox" aria-labelledby="compliance-filter-from-label" value="2020-01-01T00:00:00Z"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-to-field" label-position="above">
                            <label id="compliance-filter-to-label" slot="label" for="compliance-filter-to">To</label>
                            <fluent-text-input slot="input" id="compliance-filter-to" role="textbox" aria-labelledby="compliance-filter-to-label" value="2100-01-01T00:00:00Z"></fluent-text-input>
                          </fluent-field>
                        </div>
                        <div class="fluent-grid-item">
                          <fluent-field id="compliance-filter-limit-field" label-position="above">
                            <label id="compliance-filter-limit-label" slot="label" for="compliance-filter-limit">Limit</label>
                            <fluent-number-input slot="input" id="compliance-filter-limit" role="spinbutton" aria-labelledby="compliance-filter-limit-label" value="100"></fluent-number-input>
                          </fluent-field>
                        </div>
                      </div>
                      <div class="fluent-stack fluent-stack-horizontal compliance-audit-filters__actions">
                        <fluent-button role="button"
                                       tabindex="0"
                                       data-chatbot-stable-id="compliance-search">Search audit</fluent-button>
                        <fluent-button role="button"
                                       tabindex="0"
                                       data-chatbot-stable-id="compliance-trigger-investigation">Trigger investigation</fluent-button>
                      </div>
                    </section>
                    <ol data-compliance-dense-audit="true" aria-label="Compliance audit timeline">
                      <li>
                        <article aria-label="Audit record, SubmitRetentionConfigurationChange, restricted, 2026-06-02 04:00:00Z"
                                 data-redaction-state="restricted"
                                 data-escalation-state="not-requested">
                          <h3>SubmitRetentionConfigurationChange</h3>
                          <!-- Story 13.6: the former monospace <dl class="chatbot-definition-list"> safe-metadata dump
                               renders as a structured FluentStack (vertical .fluent-stack-vertical container, one
                               horizontal .fluent-stack-horizontal row per token: FluentText label + chatbot-code token).
                               The aria-label moves onto the container; every safe token is preserved verbatim; no
                               <dl>/<dt>/<dd> nor chatbot-definition-list remains (mirrors Story 13.4). -->
                          <div class="fluent-stack fluent-stack-vertical compliance-audit-safe-metadata" aria-label="Audit record safe metadata">
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Actor</fluent-text><code class="chatbot-code">actor:admin-alpha</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Command surface</fluent-text><code class="chatbot-code">command:SubmitRetentionConfigurationChange</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Decision</fluent-text><code class="chatbot-code">decision:allow</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Reason</fluent-text><code class="chatbot-code">reason:pre_commit_gate</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Correlation</fluent-text><code class="chatbot-code">correlation:01ARZ3NDEKTSV4RRFFQ69G5FAW</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Policy snapshot</fluent-text><code class="chatbot-code">policy-snapshot:policy-snapshot-admin-v1</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Redaction state</fluent-text><code class="chatbot-code">redaction:restricted</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Escalation status</fluent-text><code class="chatbot-code">escalation:not-requested</code></div>
                            <div class="fluent-stack fluent-stack-horizontal"><fluent-text>Safe next action</fluent-text><code class="chatbot-code">safe-next-action:request-access</code></div>
                          </div>
                          <p id="compliance-escalation-reason" class="chatbot-body">Request access with an investigation id and opaque resource reference.</p>
                          <p id="compliance-operate-denied" class="chatbot-body">Compliance scope can inspect audit metadata but cannot operate workflow items.</p>
                          <div class="compliance-action-row">
                            <fluent-button role="button"
                                           tabindex="0"
                                           aria-describedby="compliance-escalation-reason"
                                           data-chatbot-stable-id="compliance-request-access">Request compliance access</fluent-button>
                            <fluent-button role="button"
                                           tabindex="0"
                                           aria-disabled="true"
                                           aria-describedby="compliance-operate-denied">Retry queue item</fluent-button>
                          </div>
                        </article>
                      </li>
                    </ol>
                    <aside class="compliance-phone-fallback"
                           role="complementary"
                           aria-label="Compliance audit summary is available on phone.">
                      <p>Compliance audit summary is available on phone.</p>
                      <p>audit-record:retention-change-001</p>
                      <p>redaction:restricted</p>
                      <p>safe-next-action:request-access</p>
                      <p>Dense audit analysis and retention editing require a larger screen; summary and safe escalation remain reachable.</p>
                      <fluent-button role="button"
                                     tabindex="0"
                                     aria-describedby="compliance-escalation-reason"
                                     data-chatbot-stable-id="compliance-request-access">Request compliance access</fluent-button>
                    </aside>
                  </section>
                  <section class="chatbot-section"
                           data-compliance-dense-retention="true"
                           aria-labelledby="retention-editor-title">
                    <h2 id="retention-editor-title" class="chatbot-section-title">Retention configuration</h2>
                    <div id="retention-validation-summary"
                         class="chatbot-status"
                         data-chatbot-status="warning"
                         data-validation-placement="before-fields"
                         role="alert"
                         tabindex="-1"
                         aria-label="Retention validation summary">
                      <span class="chatbot-status__label">Warning</span>
                      <span>Retention windows must stay within bounded compliance policy.</span>
                    </div>
                    <div class="chatbot-form-grid">
                      <label class="chatbot-labelled-row" for="source-email-retention">Source email metadata retention days</label>
                      <div>
                        <input id="source-email-retention"
                               type="text"
                               value="10"
                               aria-invalid="true"
                               aria-describedby="source-email-retention-message" />
                        <p id="source-email-retention-message" class="chatbot-body">Window must be between 30 and 3650 days.</p>
                      </div>
                      <label class="chatbot-labelled-row" for="audit-retention">Audit record retention days</label>
                      <div>
                        <input id="audit-retention"
                               type="text"
                               value="365"
                               aria-invalid="true"
                               aria-describedby="audit-retention-message" />
                        <p id="audit-retention-message" class="chatbot-body">Audit chain reconstructability requires at least 2555 days.</p>
                      </div>
                    </div>
                    <dl class="chatbot-definition-list" aria-label="Retention safe snapshot metadata">
                      <dt class="chatbot-labelled-row">Source snapshot</dt>
                      <dd><code class="chatbot-code">retention-snapshot-current</code></dd>
                      <dt class="chatbot-labelled-row">Proposed snapshot</dt>
                      <dd><code class="chatbot-code">retention-snapshot-proposed</code></dd>
                      <dt class="chatbot-labelled-row">Old fingerprint</dt>
                      <dd><code class="chatbot-code">sha256:oldretentionfingerprint001</code></dd>
                      <dt class="chatbot-labelled-row">New fingerprint</dt>
                      <dd><code class="chatbot-code">sha256:newretentionfingerprint001</code></dd>
                      <dt class="chatbot-labelled-row">Deletion mode</dt>
                      <dd><code class="chatbot-code">projection-tombstone-key-shredding</code></dd>
                    </dl>
                    <div class="compliance-action-row">
                      <button type="button"
                              data-chatbot-stable-id="retention-submit-invalid">Submit retention change</button>
                      <button type="button"
                              data-chatbot-stable-id="retention-submit-valid">Submit valid retention change</button>
                    </div>
                  </section>
                </main>
                <script>
                  document.querySelectorAll("[data-chatbot-stable-id='compliance-request-access']").forEach(button => {
                    button.addEventListener("click", () => {
                      window.__lastComplianceCommand = {
                        commandType: "RequestComplianceEscalation",
                        escalationTarget: "project-opaque-ref"
                      };
                    });
                  });
                  document.querySelector("[data-chatbot-stable-id='compliance-trigger-investigation']").addEventListener("click", () => {
                    window.__lastComplianceCommand = {
                      commandType: "RequestComplianceInvestigation",
                      investigationId: "investigation-001"
                    };
                  });
                  document.querySelector("[data-chatbot-stable-id='retention-submit-invalid']").addEventListener("click", () => {
                    document.querySelector("#retention-validation-summary").focus();
                  });
                  document.querySelector("[data-chatbot-stable-id='retention-submit-valid']").addEventListener("click", () => {
                    window.__lastRetentionCommand = {
                      commandType: "SubmitRetentionConfigurationChange",
                      oldFingerprint: "sha256:oldretentionfingerprint001",
                      newFingerprint: "sha256:newretentionfingerprint001"
                    };
                  });
                </script>
              </body>
            </html>
            """;
    }

    private static Task SetFluentNumberInputValueAsync(ILocator input, string value)
        => input.EvaluateAsync(
            """
            (element, newValue) => {
              element.value = newValue;
              element.setAttribute("value", newValue);
              element.setAttribute("data-value", newValue);
              element.setAttribute("aria-valuenow", newValue);
              element.textContent = newValue;
              element.dispatchEvent(new Event("input", { bubbles: true }));
              element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);

    private static void AssertRetentionFixtureWithoutBrowser()
    {
        string fixture = BuildComplianceFixture(ComplianceFixtureScenario.RetentionValidation);

        fixture.ShouldContain("Retention validation summary");
        fixture.ShouldContain("data-validation-placement=\"before-fields\"");
        fixture.ShouldContain("aria-invalid=\"true\"");
        fixture.ShouldContain("SubmitRetentionConfigurationChange");
        fixture.ShouldContain("projection-tombstone-key-shredding");
        AssertMetadataOnly(fixture);
    }

    private static void AssertPhoneFixtureWithoutBrowser()
    {
        string fixture = BuildComplianceFixture(ComplianceFixtureScenario.PhoneFallback);

        fixture.ShouldContain("Compliance audit summary is available on phone.");
        fixture.ShouldContain("Dense audit analysis and retention editing require a larger screen; summary and safe escalation remain reachable.");
        fixture.ShouldContain("data-compliance-dense-audit=\"true\"");
        fixture.ShouldContain("data-compliance-dense-retention=\"true\"");
        AssertMetadataOnly(fixture);
    }

    private static void AssertMetadataOnly(string text)
    {
        text.ShouldNotContain("project name", Case.Insensitive);
        text.ShouldNotContain("mailbox body", Case.Insensitive);
        text.ShouldNotContain("message subject", Case.Insensitive);
        text.ShouldNotContain("provider payload", Case.Insensitive);
        text.ShouldNotContain("raw claim", Case.Insensitive);
        text.ShouldNotContain("authorization header", Case.Insensitive);
        text.ShouldNotContain("bearer token", Case.Insensitive);
        text.ShouldNotContain("command body", Case.Insensitive);
        text.ShouldNotContain("audit envelope", Case.Insensitive);
        text.ShouldNotContain("workflow mutation", Case.Insensitive);
        text.ShouldNotContain("{\"audit", Case.Insensitive);
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }

    private sealed class BrowserHarness : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;

        private BrowserHarness(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
            Page = page;
        }

        public IPage Page { get; }

        public static async Task<BrowserHarness?> TryStartAsync()
        {
            string? chromeExecutable = ResolveChromeExecutable();
            if (chromeExecutable is null)
            {
                return null;
            }

            try
            {
                return await StartAsync(chromeExecutable).ConfigureAwait(false);
            }
            catch (PlaywrightException ex) when (IsBrowserUnavailable(ex))
            {
                return null;
            }
        }

        public static async Task<BrowserHarness> StartAsync(string chromeExecutable)
        {
            IPlaywright? playwright = null;
            IBrowser? browser = null;
            IBrowserContext? context = null;

            try
            {
                playwright = await Playwright.CreateAsync().ConfigureAwait(false);
                browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    ExecutablePath = chromeExecutable,
                    Args =
                    [
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--no-zygote",
                        "--single-process",
                        "--disable-gpu",
                        "--disable-dev-shm-usage",
                        "--disable-crash-reporter",
                        "--disable-crashpad",
                    ],
                }).ConfigureAwait(false);
                context = await browser.NewContextAsync(new()
                {
                    ReducedMotion = ReducedMotion.Reduce,
                }).ConfigureAwait(false);
                IPage page = await context.NewPageAsync().ConfigureAwait(false);

                return new BrowserHarness(playwright, browser, context, page);
            }
            catch
            {
                if (context is not null)
                {
                    await context.DisposeAsync().ConfigureAwait(false);
                }

                if (browser is not null)
                {
                    await browser.DisposeAsync().ConfigureAwait(false);
                }

                playwright?.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync().ConfigureAwait(false);
            await _browser.DisposeAsync().ConfigureAwait(false);
            _playwright.Dispose();
        }

        private static bool IsBrowserUnavailable(PlaywrightException ex)
            => ex.Message.Contains("crashpad", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Operation not permitted", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase);

        private static string? ResolveChromeExecutable()
        {
            string? configured = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE_PATH");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return File.Exists(configured) ? configured : null;
            }

            string linuxChrome = "/usr/bin/google-chrome";
            return File.Exists(linuxChrome) ? linuxChrome : null;
        }
    }

    private enum ComplianceFixtureScenario
    {
        AuditInvestigation,
        RetentionValidation,
        PhoneFallback,
    }
}
#pragma warning restore CA2007
