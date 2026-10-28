using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.ChatBot.UI.Components.Governed;

/// <summary>Shared, encoded metadata rendering for association review surfaces.</summary>
internal static class AssociationReviewRendering
{
    /// <summary>Renders an opaque identifier or wire token as a code value.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already authorized value.</param>
    /// <returns>The Fluent key-value row.</returns>
    internal static RenderFragment CodeRow(string label, string? value) => Row(label, value, isCode: true);

    /// <summary>Renders localized display text with Fluent typography.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already authorized display value.</param>
    /// <returns>The Fluent key-value row.</returns>
    internal static RenderFragment TextRow(string label, string? value) => Row(label, value, isCode: false);

    /// <summary>Withholds both kind and reference unless evidence is available.</summary>
    /// <param name="evidence">The server-authorized evidence metadata.</param>
    /// <param name="text">The active phrase localizer.</param>
    /// <returns>Available metadata or the localized restricted-evidence placeholder.</returns>
    internal static string EvidenceText(AssociationEvidenceModel evidence, ChatBotUiTextLocalizer text)
        => evidence.State is ChatBotEvidenceState.Available
            ? string.Concat(evidence.Kind, " ", evidence.Reference)
            : text[ChatBotUiTextKey.AssociationReviewEvidenceRestricted];

    private static RenderFragment Row(string label, string? value, bool isCode) => builder =>
    {
        builder.OpenComponent<FluentStack>(0);
        builder.AddAttribute(1, nameof(FluentStack.Orientation), Orientation.Horizontal);
        builder.AddAttribute(2, nameof(FluentStack.HorizontalGap), "0.5rem");
        builder.AddAttribute(3, nameof(FluentStack.VerticalAlignment), VerticalAlignment.Center);
        builder.AddAttribute(4, nameof(FluentStack.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<FluentText>(0);
            content.AddAttribute(1, nameof(FluentText.Weight), TextWeight.Semibold);
            content.AddAttribute(2, nameof(FluentText.ChildContent), (RenderFragment)(text => text.AddContent(0, label)));
            content.CloseComponent();
            if (isCode)
            {
                content.OpenElement(3, "code");
                content.AddAttribute(4, "class", "chatbot-code");
                content.AddContent(5, value);
                content.CloseElement();
            }
            else
            {
                content.OpenComponent<FluentText>(6);
                content.AddAttribute(7, nameof(FluentText.ChildContent), (RenderFragment)(text => text.AddContent(0, value)));
                content.CloseComponent();
            }
        }));
        builder.CloseComponent();
    };
}
