using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Association.Scoring;

internal sealed record ProjectAssociationCandidateEvidence(
    string ProjectId,
    string? DisplayName,
    IReadOnlyList<AssociationDeterministicSignal> Signals);
