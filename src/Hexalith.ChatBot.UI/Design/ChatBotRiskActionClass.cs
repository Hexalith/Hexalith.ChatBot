namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Risk classes used to explain why a governed action needs review.
/// </summary>
public enum ChatBotRiskActionClass
{
    ExternallyVisible,
    FileExposing,
    ProjectMutating,
    ToolInvoking,
    TaskCreating,
    ParticipantRepresenting,
}
