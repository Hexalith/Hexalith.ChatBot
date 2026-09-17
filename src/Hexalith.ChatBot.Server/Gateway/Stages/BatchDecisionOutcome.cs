using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>The per-item result of fanning out a batch decision: either a dispatchable command or a safe denial.</summary>
/// <param name="ApprovalId">The underlying approval id (safe ref).</param>
/// <param name="Accepted">Whether the item will be dispatched as its own governed decision command.</param>
/// <param name="Command">The single-item decision command to dispatch, or <see langword="null"/> when denied.</param>
/// <param name="ReasonCode">The safe per-item reason code (no existence leakage).</param>
internal sealed record BatchDecisionOutcome(
    string ApprovalId,
    bool Accepted,
    IChatBotCommand? Command,
    string ReasonCode);
