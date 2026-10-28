using Microsoft.AspNetCore.Components;

namespace Hexalith.ChatBot.UI.Components.Governed;

/// <summary>Composes authorized metadata through the shared Fluent row component.</summary>
internal static class ChatBotMetadataRendering
{
    /// <summary>Renders an opaque identifier or wire token.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already-authorized value.</param>
    /// <returns>The shared row.</returns>
    internal static RenderFragment CodeRow(string label, string? value) => Row(label, value);

    /// <summary>Renders localized display text.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already-authorized display value.</param>
    /// <returns>The shared row.</returns>
    internal static RenderFragment TextRow(string label, string? value) => Row(label, value, isCode: false);

    /// <summary>Renders an opaque value only when nonblank.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already-authorized value.</param>
    /// <returns>The shared row, or nothing for a blank value.</returns>
    internal static RenderFragment? CodeRowIf(string label, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : CodeRow(label, value);

    /// <summary>Renders display text only when nonblank.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The already-authorized display value.</param>
    /// <returns>The shared row, or nothing for a blank value.</returns>
    internal static RenderFragment? TextRowIf(string label, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : TextRow(label, value);

    /// <summary>Renders a localized description beside its opaque token.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="text">The localized description.</param>
    /// <param name="token">The already-authorized token.</param>
    /// <returns>The shared row.</returns>
    internal static RenderFragment LabelTextCodeRow(string label, string? text, string? token)
        => Row(label, token, text: text);

    /// <summary>Renders a culture-formatted timestamp and invariant machine value.</summary>
    /// <param name="label">The localized field label.</param>
    /// <param name="value">The timestamp.</param>
    /// <param name="formattedValue">The culture-formatted display value.</param>
    /// <returns>The shared row.</returns>
    internal static RenderFragment TimeRow(string label, DateTimeOffset value, string formattedValue)
        => Row(label, formattedValue, timestamp: value);

    private static RenderFragment Row(string label, string? value, bool isCode = true, string? text = null, DateTimeOffset? timestamp = null) => builder =>
    {
        builder.OpenComponent<ChatBotMetadataRow>(0);
        builder.AddAttribute(1, nameof(ChatBotMetadataRow.Label), label);
        builder.AddAttribute(2, nameof(ChatBotMetadataRow.Value), value);
        builder.AddAttribute(3, nameof(ChatBotMetadataRow.IsCode), isCode);
        builder.AddAttribute(4, nameof(ChatBotMetadataRow.Text), text);
        builder.AddAttribute(5, nameof(ChatBotMetadataRow.Timestamp), timestamp);
        builder.CloseComponent();
    };
}
