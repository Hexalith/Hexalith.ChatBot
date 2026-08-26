namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// An AI response generation that has started and has not yet reached a terminal outcome. Tracked by
/// <see cref="GovernedOperationState"/> so the governed Stop/Cancel handler can bind a cancellation to a real,
/// still-active generation before mutating anything, rather than accepting whatever identity the client sends.
/// </summary>
/// <param name="ResponseId">The proposal id the project-conversation projection exposes as the response id.</param>
/// <param name="GenerationId">The execution id the projection exposes as the generation id.</param>
/// <param name="ProjectId">The project the generation belongs to; a cancellation must name the same one.</param>
/// <param name="StartedSourceVersion">
/// The proposal source version the generation started from. A cancellation asserting a version below this is
/// definitionally stale.
/// </param>
/// <param name="CorrelationId">The generation's correlation id, retained for audit continuity.</param>
public sealed record ActiveAiResponseGeneration(
    string ResponseId,
    string GenerationId,
    string ProjectId,
    long StartedSourceVersion,
    string CorrelationId,
    string TenantId = "unavailable",
    string? ConversationId = null,
    string Status = "active",
    string? CancellationId = null);
