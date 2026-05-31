namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// EventStore wiring constants for the ChatBot bounded context. The domain name is the second segment
/// of the EventStore aggregate identity (<c>{tenant}:chatbot:{aggregateId}</c>) and matches the DAPR
/// app id (<see cref="Hexalith.ChatBot.Contracts.ChatBotModuleInfo.DaprAppId"/>).
/// </summary>
internal static class ChatBotEventStore
{
    /// <summary>The EventStore domain name for ChatBot aggregates.</summary>
    public const string DomainName = "chatbot";
}
