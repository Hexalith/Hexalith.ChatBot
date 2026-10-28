using System.Net;
using System.Text.RegularExpressions;

using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewRenderingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("candidate-required")]
    [InlineData("correction-target-required")]
    public async Task RepeatedActionPanelsShouldKeepLabelsAndDescriptionsLocalAndStable(string? validation)
    {
        string html = await RenderPairAsync<ChatBotAssociationReviewActions>(new()
        {
            [nameof(ChatBotAssociationReviewActions.CanCorrect)] = true,
            [nameof(ChatBotAssociationReviewActions.ValidationErrorCode)] = validation,
        });
        string[] panels = Regex.Split(html, "(?=<section class=\"chatbot-association-actions\")")
            .Where(part => part.StartsWith("<section", StringComparison.Ordinal)).ToArray();
        panels.Length.ShouldBe(2);
        AssertUniqueIds(html);
        foreach (string panel in panels)
        {
            AssertReferencesResolve(panel);
            panel.ShouldContain("class=\"chatbot-governed-action__reason\"");
            Regex.Matches(panel, "class=\"chatbot-association-action-wrap\"").Count.ShouldBe(4);
            Regex.Matches(panel, "<(?:fluent-)?label[^>]*for=\"([^\"]+)\"").Count.ShouldBe(2);
            foreach (string suffix in new[] { "association-decision-note", "association-correction-rationale" })
            {
                string field = Regex.Match(panel, $"<fluent-textarea[^>]*id=\"[^\"]*{suffix}\"[^>]*>").Value;
                field.ShouldNotBeEmpty();
                string description = Regex.Match(field, "aria-describedby=\"([^\"]+)\"").Groups[1].Value;
                description.ShouldNotBeEmpty();
                bool invalid = suffix == "association-decision-note" ? validation == "candidate-required" : validation == "correction-target-required";
                field.ShouldContain($"aria-invalid=\"{invalid.ToString().ToLowerInvariant()}\"");
                description.Contains("association-review-validation", StringComparison.Ordinal).ShouldBe(invalid);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RepeatedComparisonsShouldIsolateHeadingEmptyStateAndEvidenceIds(bool showHeading)
    {
        string empty = await RenderPairAsync<ChatBotAssociationEvidenceComparison>(new()
        {
            [nameof(ChatBotAssociationEvidenceComparison.ShowHeading)] = showHeading,
        });
        AssertUniqueIds(empty);
        AssertReferencesResolve(empty);
        Regex.Matches(empty, "<h2 ").Count.ShouldBe(showHeading ? 2 : 0);
        if (!showHeading)
        {
            Regex.Matches(empty, "<section[^>]*aria-label=\"").Count.ShouldBe(2);
        }

        AssociationCandidateModel candidate = RestrictedCandidate();
        string comparison = await RenderPairAsync<ChatBotAssociationEvidenceComparison>(new()
        {
            [nameof(ChatBotAssociationEvidenceComparison.Candidate)] = candidate,
            [nameof(ChatBotAssociationEvidenceComparison.ShowHeading)] = showHeading,
        });
        Regex.Matches(comparison, "<h2 ").Count.ShouldBe(showHeading ? 2 : 0);
        if (!showHeading)
        {
            Regex.Matches(comparison, "<section[^>]*aria-label=\"").Count.ShouldBe(2);
        }
        string rows = await RenderPairAsync<ChatBotAssociationCandidateRow>(new()
        {
            [nameof(ChatBotAssociationCandidateRow.Candidate)] = candidate,
        });
        foreach (string html in new[] { comparison, rows })
        {
            AssertUniqueIds(html);
            AssertReferencesResolve(html);
            html.ShouldNotContain("PRIVATE_REFERENCE");
            html.ShouldNotContain("PRIVATE_KIND");
            html.ShouldNotContain("PRIVATE_FINGERPRINT");
            Regex.Matches(html, "id=\"chatbot-evidence-[^\"]+-reason\"").Count.ShouldBe(4);
        }

        // Opaque values remain encoded <code>; confidence display remains ordinary Fluent text.
        comparison.ShouldContain("<code class=\"chatbot-code\">project-&lt;safe&gt;</code>");
        WebUtility.HtmlDecode(comparison).ShouldContain("Visible <candidate>");
        comparison.ShouldNotContain("<candidate>");
        string confidence = .5.ToString("P0", System.Globalization.CultureInfo.CurrentCulture);
        WebUtility.HtmlDecode(comparison).ShouldContain(confidence);
        CodeValues(comparison).ShouldNotContain(confidence);
        Regex.Matches(rows, "class=\"[^\"]*\\bchatbot-association-candidate\\b[^\"]*\"").Count.ShouldBe(2);
        Regex.Matches(rows, "class=\"chatbot-association-candidate__evidence\"").Count.ShouldBe(2);
        Regex.Matches(comparison, "class=\"chatbot-association-comparison__panel\"").Count.ShouldBe(2);
    }

    private static AssociationCandidateModel RestrictedCandidate()
    {
        AssociationEvidenceModel restricted = new("PRIVATE_REFERENCE", "PRIVATE_FINGERPRINT", "PRIVATE_KIND", ChatBotEvidenceState.Redacted, "Restricted evidence");
        return new("project-<safe>", "Visible <candidate>", .5, 1, ["safe-reason"], [restricted, restricted], false);
    }

    [Theory]
    [InlineData(ChatBotEvidenceState.Available)]
    [InlineData(ChatBotEvidenceState.Redacted)]
    [InlineData(ChatBotEvidenceState.Unauthorized)]
    [InlineData(ChatBotEvidenceState.Unavailable)]
    public async Task SharedEvidenceDisplayShouldExposeOnlyAvailableMetadata(ChatBotEvidenceState state)
    {
        AssociationCandidateModel candidate = RestrictedCandidate() with
        {
            Evidence = [new("REFERENCE<sentinel>", "FINGERPRINT_SENTINEL", "KIND<sentinel>", state, "Unavailable")],
        };
        string comparison = await RenderPairAsync<ChatBotAssociationEvidenceComparison>(new()
        {
            [nameof(ChatBotAssociationEvidenceComparison.Candidate)] = candidate,
        });
        string row = await RenderPairAsync<ChatBotAssociationCandidateRow>(new()
        {
            [nameof(ChatBotAssociationCandidateRow.Candidate)] = candidate,
        });
        foreach (string html in new[] { comparison, row })
        {
            html.Contains("REFERENCE&lt;sentinel&gt;", StringComparison.Ordinal).ShouldBe(state == ChatBotEvidenceState.Available);
            html.Contains("KIND&lt;sentinel&gt;", StringComparison.Ordinal).ShouldBe(state == ChatBotEvidenceState.Available);
            html.ShouldNotContain("FINGERPRINT_SENTINEL");
            html.ShouldNotContain("<sentinel>");
            if (state != ChatBotEvidenceState.Available)
            {
                WebUtility.HtmlDecode(html).ShouldContain(AssociationReviewTestText.Create()[ChatBotUiTextKey.AssociationReviewEvidenceRestricted]);
            }
        }
    }

    [Fact]
    public async Task CorrectionDisplayTextShouldStayEncodedAndOutsideCodeElements()
    {
        const string owner = "Owner <reviewer>";
        string html = await RenderPairAsync<ChatBotAssociationReviewActions>(new()
        {
            [nameof(ChatBotAssociationReviewActions.CanCorrect)] = true,
            [nameof(ChatBotAssociationReviewActions.ResponsibleOwnerRole)] = owner,
        });
        html.ShouldContain("Owner &lt;reviewer&gt;");
        html.ShouldNotContain("<reviewer>");
        CodeValues(html).ShouldNotContain(owner);
    }

    [Fact]
    public async Task RevealingCorrectionAndValidationShouldPreserveSurvivingIdsAndLocalReferences()
    {
        await RenderPairAsync<ChatBotAssociationReviewActions>(new(), new()
        {
            [nameof(ChatBotAssociationReviewActions.CanCorrect)] = true,
            [nameof(ChatBotAssociationReviewActions.ValidationErrorCode)] = "correction-target-required",
        }, (before, after) =>
        {
            AssertSurvivingIds(before, after, "association-actions-title", "association-decision-note", "association-decision-note-counter");
            foreach (string panel in Regex.Split(after, "(?=<section class=\"chatbot-association-actions\")")
                .Where(part => part.StartsWith("<section", StringComparison.Ordinal)))
            {
                AssertReferencesResolve(panel);
            }
        });
    }

    [Fact]
    public async Task CandidateAndEvidenceChangesShouldPreserveInstanceIdentity()
    {
        AssociationCandidateModel candidate = RestrictedCandidate();
        await RenderPairAsync<ChatBotAssociationEvidenceComparison>(new(), new()
        {
            [nameof(ChatBotAssociationEvidenceComparison.Candidate)] = candidate,
        }, (before, after) => AssertSurvivingIds(before, after, "association-comparison-title"));

        await RenderPairAsync<ChatBotAssociationCandidateRow>(new()
        {
            [nameof(ChatBotAssociationCandidateRow.Candidate)] = candidate,
        }, new()
        {
            [nameof(ChatBotAssociationCandidateRow.Candidate)] = candidate with { ProjectId = "next-project", Evidence = [candidate.Evidence[0]] },
        }, (before, after) =>
        {
            AssertSurvivingIds(before, after, "evidence-0-reason");
            AssertReferencesResolve(after);
            after.ShouldNotContain("evidence-1-reason");
        });
    }

    private static async Task<string> RenderPairAsync<T>(Dictionary<string, object?> parameters,
        Dictionary<string, object?>? updated = null, Action<string, string>? assertTransition = null)
        where T : IComponent
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddFluentUIComponents();
        services.AddSingleton<IJSRuntime, AssociationRenderJsRuntime>();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        services.AddSingleton<ChatBotCultureFormatter>();
        services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
        ServiceProvider provider = services.BuildServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
        List<IComponent> instances = [];
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<FluentStack>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(FluentStack.ChildContent)] = (RenderFragment)(builder =>
                {
                    for (int index = 0; index < 2; index++)
                    {
                        builder.OpenComponent<T>(0);
                        foreach ((string name, object? value) in parameters)
                        {
                            builder.AddAttribute(1, name, value);
                        }
                        builder.AddComponentReferenceCapture(2, instance => instances.Add((IComponent)instance));
                        builder.CloseComponent();
                    }
                }),
            })).ConfigureAwait(true);
            string before = root.ToHtmlString();
            foreach (IComponent instance in instances)
            {
                await instance.SetParametersAsync(ParameterView.Empty).ConfigureAwait(true);
            }

            string after = root.ToHtmlString();
            Ids(after).ShouldBe(Ids(before), "component IDs must survive rerenders");
            if (updated is not null)
            {
                foreach (IComponent instance in instances)
                {
                    await instance.SetParametersAsync(ParameterView.FromDictionary(updated)).ConfigureAwait(true);
                }

                after = root.ToHtmlString();
                AssertUniqueIds(after);
                assertTransition?.Invoke(before, after);
            }

            return after;
        }).ConfigureAwait(true);
    }

    private static string[] Ids(string html)
        => Regex.Matches(html, "(?<![\\w-])id=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();

    private static string[] CodeValues(string html)
        => Regex.Matches(html, "<code[^>]*>(.*?)</code>").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value)).ToArray();

    private static void AssertSurvivingIds(string before, string after, params string[] suffixes)
    {
        foreach (string suffix in suffixes)
        {
            string[] original = Ids(before).Where(id => id.EndsWith(suffix, StringComparison.Ordinal)).ToArray();
            original.Length.ShouldBe(2);
            Ids(after).Where(id => id.EndsWith(suffix, StringComparison.Ordinal)).ShouldBe(original);
        }
    }

    private static void AssertUniqueIds(string html)
    {
        string[] ids = Ids(html);
        ids.ShouldNotBeEmpty();
        ids.Distinct(StringComparer.Ordinal).Count().ShouldBe(ids.Length);
    }

    private static void AssertReferencesResolve(string html)
    {
        string[] ids = Ids(html);
        foreach (Match attribute in Regex.Matches(html, "(?:aria-labelledby|aria-describedby|for)=\"([^\"]+)\""))
        {
            foreach (string reference in attribute.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                ids.Count(id => id == reference).ShouldBe(1, $"{reference} must resolve once within this component");
            }
        }
    }
}
