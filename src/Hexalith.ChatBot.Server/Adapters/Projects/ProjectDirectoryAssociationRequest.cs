using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Scoring;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

internal sealed record ProjectDirectoryAssociationRequest(
    string TenantId,
    string SourceConversationId,
    string? SourceThreadId,
    IReadOnlyList<AssociationDeterministicSignal> Signals,
    string CorrelationId);
