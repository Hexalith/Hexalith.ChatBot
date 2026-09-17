using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>The plan produced for a batch approve/reject request.</summary>
/// <param name="Authorized">
/// <see langword="false"/> when the actor is denied batch approval before state load (non-human actor); then
/// <see cref="Outcomes"/> is empty and no item is acted on.
/// </param>
/// <param name="ReasonCode">The batch-level reason code.</param>
/// <param name="GroupKeyFingerprint">The safe <c>sha256:</c> group fingerprint carried into each per-item audit envelope.</param>
/// <param name="Outcomes">The per-item outcomes (empty when the batch is denied before state load).</param>
internal sealed record BatchDecisionPlan(
    bool Authorized,
    string ReasonCode,
    string GroupKeyFingerprint,
    IReadOnlyList<BatchDecisionOutcome> Outcomes)
{
    public int AcceptedCount => Outcomes.Count(static outcome => outcome.Accepted);

    public int DeniedCount => Outcomes.Count(static outcome => !outcome.Accepted);

    public IEnumerable<IChatBotCommand> Commands => Outcomes
        .Where(static outcome => outcome is { Accepted: true, Command: not null })
        .Select(static outcome => outcome.Command!);
}
