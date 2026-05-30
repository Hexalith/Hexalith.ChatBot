using Hexalith.ChatBot.Contracts;

namespace Hexalith.ChatBot.Client;

public sealed record ChatBotClientDescriptor(string ModuleName, string DaprAppId)
{
    public static ChatBotClientDescriptor Default { get; } = new(
        ChatBotModuleInfo.ModuleName,
        ChatBotModuleInfo.DaprAppId);
}
