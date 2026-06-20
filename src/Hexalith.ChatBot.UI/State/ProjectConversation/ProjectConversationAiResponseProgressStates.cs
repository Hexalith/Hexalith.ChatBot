namespace Hexalith.ChatBot.UI.State.ProjectConversation;

internal static class ProjectConversationAiResponseProgressStates
{
    public const string Pending = "pending";
    public const string Rendering = "rendering";
    public const string Cancelling = "cancelling";
    public const string Stopped = "stopped";
    public const string Cancelled = "cancelled";

    public static bool IsActive(string? state)
        => state is Pending or Rendering or Cancelling;

    public static bool IsVerifiedStop(string? state)
        => state is Stopped or Cancelled;
}
