using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>The underlying decision-command family a grouped approval item fans out to.</summary>
internal enum BatchDecisionItemKind
{
    AiAction,
    Outbound,
}
