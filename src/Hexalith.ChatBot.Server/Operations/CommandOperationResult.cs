using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>A pure evaluation result retaining the unchanged state on rejection or replay.</summary>
/// <param name="State">The resulting or unchanged state.</param>
/// <param name="Conflict">The typed metadata-only rejection.</param>
/// <param name="IsReplay">Whether the exact stored state was returned.</param>
internal sealed record CommandOperationResult(CommandOperationState? State, CommandOperationConflict Conflict = CommandOperationConflict.None, bool IsReplay = false)
{
    /// <summary>Gets whether evaluation succeeded.</summary>
    public bool Succeeded => Conflict == CommandOperationConflict.None;

    /// <summary>Gets only a catalog-owned safe reason.</summary>
    public string ReasonCode => Succeeded ? State!.Transitions[^1].ReasonCode : ChatBotMessageCodes.IdempotencyConflictCommandExecution;
}
