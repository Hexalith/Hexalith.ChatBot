using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Microsoft.AspNetCore.Components;

namespace Hexalith.ChatBot.UI.Components.Governed;

/// <summary>Shared, encoded metadata rendering for association review surfaces.</summary>
internal static class AssociationReviewRendering
{
    /// <summary>Renders an opaque identifier or wire token as a code value.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already authorized value.</param>
    /// <returns>The Fluent key-value row.</returns>
    internal static RenderFragment CodeRow(string label, string? value) => ChatBotMetadataRendering.CodeRow(label, value);

    /// <summary>Renders localized display text with Fluent typography.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already authorized display value.</param>
    /// <returns>The Fluent key-value row.</returns>
    internal static RenderFragment TextRow(string label, string? value) => ChatBotMetadataRendering.TextRow(label, value);

    /// <summary>Withholds both kind and reference unless evidence is available.</summary>
    /// <param name="evidence">The server-authorized evidence metadata.</param>
    /// <param name="text">The active phrase localizer.</param>
    /// <returns>Available metadata or the localized restricted-evidence placeholder.</returns>
    internal static string EvidenceText(AssociationEvidenceModel evidence, ChatBotUiTextLocalizer text)
        => evidence.State is ChatBotEvidenceState.Available
            ? string.Concat(evidence.Kind, " ", evidence.Reference)
            : text[ChatBotUiTextKey.AssociationReviewEvidenceRestricted];
}
