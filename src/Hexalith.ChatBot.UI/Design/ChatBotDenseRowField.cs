namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Label and retention metadata for a field in a dense governed row.
/// </summary>
/// <param name="Label">Visible field label.</param>
/// <param name="Retention">Responsive retention policy.</param>
public sealed record ChatBotDenseRowField(string Label, ChatBotDenseRowFieldRetention Retention);
