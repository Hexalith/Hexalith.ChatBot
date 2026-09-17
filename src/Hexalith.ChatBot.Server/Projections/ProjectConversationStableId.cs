using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Intake;

namespace Hexalith.ChatBot.Server.Projections;

internal static class ProjectConversationStableId
{
    public static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    }
}
