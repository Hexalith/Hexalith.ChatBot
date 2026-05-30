namespace Hexalith.ChatBot.Contracts.Messages;

public static class ChatBotDisabledActionReasons
{
    public const string InsufficientAuthority = "insufficient-authority";
    public const string StateNotPermitted = "state-not-permitted";
    public const string DependencyDegraded = "dependency-degraded";
    public const string AwaitingOtherActor = "awaiting-other-actor";
    public const string PolicyBlocked = "policy-blocked";
}
