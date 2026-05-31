namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Action metadata used to enforce one-primary-action workflow items.
/// </summary>
/// <param name="Label">Action label.</param>
/// <param name="Kind">Action grouping kind.</param>
public sealed record ChatBotWorkflowItemActionContract(string Label, ChatBotWorkflowItemActionKind Kind)
{
    /// <summary>Gets a value indicating whether the action metadata is complete.</summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(Label);
}
