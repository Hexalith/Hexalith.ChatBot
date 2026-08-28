namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationStoreActivityInput(
    CorrectionPropagationRequest Request,
    string StoreKey,
    DateTimeOffset StartedAtUtc,
    string? RemoteOperationId = null);
