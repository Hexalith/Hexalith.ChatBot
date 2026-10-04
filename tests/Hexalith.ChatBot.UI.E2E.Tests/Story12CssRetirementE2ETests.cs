using System.Text.RegularExpressions;

using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

public sealed class Story12CssRetirementE2ETests
{
    private static readonly Regex NativeControlCssSelector = new(
        "(^|[\\s,{>+~])(?:button|input|select|textarea)(?=[:.#\\s,{>+~\\[]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    [Fact]
    public void RetiredPresentationHooksShouldNotRemainInProductionSourceOrE2EFixtures()
    {
        string[] forbiddenHooks =
        [
            "chatbot-action-button",
            "chatbot-governed-composer__input",
            "chatbot-association-actions__input",
            "chatbot-actor-badge__action",
            "chatbot-why-project-panel__close",
            "chatbot-why-project-panel__correction",
        ];

        string[] scanRoots =
        [
            "src/Hexalith.ChatBot.UI/Components",
            "src/Hexalith.ChatBot.UI/wwwroot/css",
            "tests/Hexalith.ChatBot.UI.E2E.Tests",
        ];

        List<string> offenders = [];

        foreach (string file in scanRoots.SelectMany(EnumerateProjectFiles))
        {
            if (Path.GetFileName(file).Equals($"{nameof(Story12CssRetirementE2ETests)}.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string content = File.ReadAllText(file);
            foreach (string hook in forbiddenHooks)
            {
                if (content.Contains(hook, StringComparison.Ordinal))
                {
                    offenders.Add($"{RelativeProjectPath(file)} contains {hook}");
                }
            }
        }

        offenders.ShouldBeEmpty(
            "Story 12.8 retired these presentation classes; stable E2E hooks must use ids, aria attributes, "
            + $"or data-chatbot-* markers instead. Offenders: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void RetiredCssPrimitiveSelectorsShouldStayAbsentFromProductionStylesheet()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        foreach (string forbidden in new[]
        {
            "--chatbot-type-",
            "--chatbot-font-",
            "--chatbot-radius-",
            "--chatbot-color-",
            ".chatbot-button",
        })
        {
            css.ShouldNotContain(forbidden, Case.Sensitive);
        }

        NativeControlCssSelector.Matches(css).Count.ShouldBe(0);
        css.ShouldContain("var(--colorNeutralStroke1)", Case.Sensitive);
        css.ShouldContain("@media (forced-colors: active)", Case.Sensitive);
        css.ShouldContain("@media (prefers-reduced-motion: reduce)", Case.Sensitive);
    }

    [Fact]
    public void RetiredControlClassesShouldBeReplacedByFluentAndSemanticBehaviorContracts()
    {
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string composer = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor");
        string associationActions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string actorBadge = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor");
        string whyPanel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor");

        approval.ShouldContain("<FluentButton", Case.Sensitive);
        approval.ShouldContain("Appearance=\"ButtonAppearance.Primary\"", Case.Sensitive);
        approval.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"", Case.Sensitive);
        approval.ShouldContain("aria-describedby=\"@(!CanApprove ? ApproveReasonId : null)\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"ApproveAsync\"", Case.Sensitive);
        approval.ShouldNotContain("chatbot-action-button", Case.Sensitive);

        composer.ShouldContain("<FluentTextArea", Case.Sensitive);
        composer.ShouldContain("Id=\"project-conversation-composer-input\"", Case.Sensitive);
        composer.ShouldContain("aria-describedby=\"project-conversation-composer-help project-conversation-composer-status\"", Case.Sensitive);
        composer.ShouldContain("<FluentButton", Case.Sensitive);
        composer.ShouldNotContain("chatbot-governed-composer__input", Case.Sensitive);

        associationActions.ShouldContain("<FluentTextArea", Case.Sensitive);
        associationActions.ShouldContain("Id=\"association-decision-note\"", Case.Sensitive);
        associationActions.ShouldContain("Id=\"association-correction-rationale\"", Case.Sensitive);
        associationActions.ShouldContain("aria-invalid=\"@DecisionNoteInvalidText\"", Case.Sensitive);
        associationActions.ShouldContain("aria-describedby=\"@DecisionNoteDescribedBy\"", Case.Sensitive);
        associationActions.ShouldContain("aria-describedby=\"@CorrectionRationaleDescribedBy\"", Case.Sensitive);
        associationActions.ShouldNotContain("chatbot-association-actions__input", Case.Sensitive);

        actorBadge.ShouldContain("<FluentBadge", Case.Sensitive);
        actorBadge.ShouldContain("aria-label=\"@AccessibleName\"", Case.Sensitive);
        actorBadge.ShouldContain("data-chatbot-actor-category", Case.Sensitive);
        actorBadge.ShouldNotContain("chatbot-actor-badge__action", Case.Sensitive);

        whyPanel.ShouldContain("<FluentButton", Case.Sensitive);
        whyPanel.ShouldContain("aria-label=\"@UiText[ChatBotUiTextKey.WhyProjectCloseAction]\"", Case.Sensitive);
        whyPanel.ShouldContain("data-chatbot-correction-link", Case.Sensitive);
        whyPanel.ShouldContain("data-chatbot-why-project-panel=\"metadata-only\"", Case.Sensitive);
        whyPanel.ShouldNotContain("chatbot-why-project-panel__close", Case.Sensitive);
        whyPanel.ShouldNotContain("chatbot-why-project-panel__correction", Case.Sensitive);
    }

    [Theory]
    [InlineData(null, "false", "false")]
    [InlineData("candidate-required", "true", "false")]
    [InlineData("correction-target-required", "false", "true")]
    public async Task AssociationValidationShouldDescribeOnlyTheRenderedFieldThatCanCauseTheError(
        string? validationCode, string decisionInvalid, string correctionInvalid)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddFluentUIComponents();
        services.AddSingleton<IJSRuntime, StaticRenderJsRuntime>();
        services.AddSingleton<ChatBotUiTextLocalizer>();
        services.AddSingleton<ChatBotCultureFormatter>();
        services.AddSingleton<ChatBotAnnouncementDeduplicationState>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<ChatBotAssociationReviewActions>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(ChatBotAssociationReviewActions.CanCorrect)] = true,
                    [nameof(ChatBotAssociationReviewActions.ValidationErrorCode)] = validationCode,
                })).ConfigureAwait(true);
            return component.ToHtmlString();
        }).ConfigureAwait(true);

        string decision = Regex.Match(html, "<fluent-textarea[^>]*id=\"association-decision-note\"[^>]*>").Value;
        string correction = Regex.Match(html, "<fluent-textarea[^>]*id=\"association-correction-rationale\"[^>]*>").Value;
        decision.ShouldNotBeEmpty(html);
        correction.ShouldNotBeEmpty();
        decision.ShouldContain($"aria-invalid=\"{decisionInvalid}\"");
        correction.ShouldContain($"aria-invalid=\"{correctionInvalid}\"");
        foreach ((string field, string invalid) in new[] { (decision, decisionInvalid), (correction, correctionInvalid) })
        {
            string description = Regex.Match(field, "aria-describedby=\"([^\"]+)\"").Groups[1].Value;
            description.ShouldNotBeEmpty();
            description.Split(' ').ShouldContain(invalid == "true" ? "association-review-validation"
                : field == decision ? "association-decision-note-counter" : "association-correction-rationale-counter");
            if (invalid == "false")
            {
                description.Split(' ').ShouldNotContain("association-review-validation");
            }

            foreach (string id in description.Split(' '))
            {
                html.ShouldContain($"id=\"{id}\"");
            }
        }
    }

    private sealed class StaticRenderJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }

    private static IEnumerable<string> EnumerateProjectFiles(string relativeRoot)
    {
        string root = ProjectPath(relativeRoot);
        Directory.Exists(root).ShouldBeTrue($"{relativeRoot} should exist.");

        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        return Directory
            .EnumerateFiles(root, "*", options)
            .Where(static file => file.EndsWith(".razor", StringComparison.Ordinal)
                || file.EndsWith(".css", StringComparison.Ordinal)
                || file.EndsWith(".cs", StringComparison.Ordinal))
            .Where(static file => !IsBuildOutput(file));
    }

    private static bool IsBuildOutput(string file)
    {
        string normalized = file.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal);
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

    private static string RelativeProjectPath(string file)
        => Path.GetRelativePath(ProjectPath("."), file).Replace('\\', '/');
}
